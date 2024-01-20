using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Solitons.Collections;

namespace Solitons.CommandLine;

/// <summary>
/// Represents a command line option that can be parsed into a type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the argument value.</typeparam>
public class ParseableOption<T> : Option<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseableOption{T}"/> class.
    /// </summary>
    /// <param name="optionDescriptor">The descriptor for the option.</param>
    [DebuggerStepThrough]
    protected ParseableOption(IOptionDescriptor optionDescriptor) 
        : base(optionDescriptor.Aliases.ToArray(), optionDescriptor.Parse, optionDescriptor.IsDefault, optionDescriptor.Description)
    {
    }

    /// <summary>
    /// Gets the parsed result for this option from the <see cref="InvocationContext"/>.
    /// </summary>
    /// <param name="context">The invocation context of the command line parser.</param>
    /// <returns>The parsed value of the option.</returns>
    [DebuggerStepThrough]
    public virtual T? GetParseResult(InvocationContext context) => context.ParseResult.GetValueForOption(this);

    /// <summary>
    /// Defines the interface for option descriptors.
    /// </summary>
    protected interface IOptionDescriptor
    {
        /// <summary>
        /// Gets the aliases for the command line option.
        /// </summary>
        IReadOnlyList<string> Aliases { get; }

        /// <summary>
        /// Gets a value indicating whether this is the default option.
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// Gets the description of the command line option.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Parses the command line argument into an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="result">The result of the command line argument parsing.</param>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        T Parse(ArgumentResult result);
    }

    /// <summary>
    /// Provides a base implementation for option descriptors.
    /// </summary>
    protected abstract class OptionDescriptor : IOptionDescriptor
    {
        private readonly ImmutableArray<string> _aliases;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionDescriptor"/> class.
        /// </summary>
        /// <param name="names">The collection of alias names for the option.</param>
        /// <param name="isDefault">Indicates whether this option is the default.</param>
        /// <param name="description">The description of the option.</param>
        protected OptionDescriptor(IEnumerable<string> names, bool isDefault, string description)
        {
            IsDefault = isDefault;
            Description = description;
            _aliases = names.Distinct(StringComparer.Ordinal).ToImmutableArray();
            if (!_aliases.Any())
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionDescriptor"/> class with a single name.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="isDefault">Indicates whether this option is the default.</param>
        /// <param name="description">The description of the option.</param>
        [DebuggerStepThrough]
        protected OptionDescriptor(string name, bool isDefault, string description) 
            : this(FluentArray.Create(name), isDefault, description){}

        /// <inheritdoc />
        public IReadOnlyList<string> Aliases => _aliases;

        /// <inheritdoc />
        public bool IsDefault { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public abstract T Parse(ArgumentResult result);
    }

}