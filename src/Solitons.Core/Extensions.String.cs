﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Solitons
{
    /// <summary>
    /// Common extensions
    /// </summary>
    public static partial class Extensions
    {
        private static readonly Lazy<Regex> Base64Regex = new(() => new Regex(@"(?xis-m)^[a-z0-9\+/]*={0,3}$"));


        /// <summary>
        /// Removes the specified prefix from the start of the source string if it exists.
        /// </summary>
        /// <param name="source">The string to process.</param>
        /// <param name="prefix">The prefix to remove.</param>
        /// <param name="comparisonType">
        /// Specifies the type of comparison to use. Defaults to <see cref="StringComparison.Ordinal"/>.
        /// </param>
        /// <returns>The source string without the specified prefix, or the original string if the prefix is not found.</returns>
        public static string RemovePrefix(this string source, string prefix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            return source.StartsWith(prefix, comparisonType)
                ? source.Substring(prefix.Length)
                : source;
        }

        /// <summary>
        /// Removes the specified suffix from the end of the source string if it exists.
        /// </summary>
        /// <param name="source">The string to process.</param>
        /// <param name="suffix">The suffix to remove.</param>
        /// <param name="comparisonType">
        /// Specifies the type of comparison to use. Defaults to <see cref="StringComparison.Ordinal"/>.
        /// </param>
        /// <returns>The source string without the specified suffix, or the original string if the suffix is not found.</returns>
        public static string RemoveSuffix(this string source, string suffix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (source.EndsWith(suffix, comparisonType))
            {
                return source.Substring(0, source.Length - suffix.Length);
            }

            return source;
        }




        /// <summary>
        /// Determines whether this string contains any number of printable characters.
        /// </summary>
        /// <param name="input">The string to test</param>
        /// <returns><c>true</c> if this string contains any number of printable characters, and <c>false</c> otherwise</returns>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrintable(this string? input) => !(string.IsNullOrWhiteSpace(input));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBase64String(this string? input) => 
            input != null &&
            input.Length % 4 == 0 &&
            Base64Regex.Value.IsMatch(input);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsBase64Bytes(this string self)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            return System.Convert.FromBase64String(self);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="createException"></param>
        /// <returns></returns>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ThrowIfNull(this string? self, Func<Exception> createException) => 
            Solitons.ThrowIf.NullReference(self, createException);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="expectedKind"></param>
        /// <param name="createException"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ThrowIfMalformedUri(this string self, UriKind expectedKind, Func<Exception> createException)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (createException == null) throw new ArgumentNullException(nameof(createException));
            if (Uri.IsWellFormedUriString(self, expectedKind)) return self;
            throw createException.Invoke() ?? throw new ArgumentNullException($"'{self}' is not a well formed {expectedKind} uri.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="expectedKind"></param>
        /// <param name="parameterName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static string ThrowIfMalformedUriArgument(this string self, UriKind expectedKind, string parameterName, string? message = null)
        {
            if (string.IsNullOrWhiteSpace(self) ||
                false == Uri.IsWellFormedUriString(self, expectedKind))
            {

                throw message.IsNullOrWhiteSpace()
                    ? new ArgumentException($"Mallformed URI argument.", nameof(parameterName))
                    : new ArgumentException(message, nameof(parameterName));
            }
            return self;
        }

        /// <summary>
        /// Throws if null or white space.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="createException">The create exception.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">createException</exception>
        /// <exception cref="System.NullReferenceException"></exception>
        [DebuggerNonUserCode]
        [return:NotNull]
        public static string ThrowIfNullOrWhiteSpace(this string? self, Func<Exception> createException)
        {
            if (createException == null) throw new ArgumentNullException(nameof(createException));
            if (string.IsNullOrWhiteSpace(self))
            {
                var error = createException.Invoke() ?? throw new NullReferenceException($"{nameof(createException)}() returned null.");
                throw error;
            }
            return self;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [DebuggerNonUserCode]
        [return: NotNull]
        public static string ThrowIfNullOrWhiteSpace(this string? self, string message)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                throw new InvalidOperationException(message);
            }
            return self;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [DebuggerNonUserCode]
        [return: NotNull]
        public static string ThrowIfNullOrWhiteSpace(this string? self)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                throw new InvalidOperationException();
            }
            return self;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="createException"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [DebuggerNonUserCode]
        [return: NotNull]
        public static string ThrowIfNullOrEmpty(this string? self, Func<Exception> createException)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                var error = createException.Invoke() ?? throw new InvalidOperationException();
                throw error;
            }
            return self;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [DebuggerNonUserCode]
        [return: NotNull]
        public static string ThrowIfNullOrEmpty(this string? self, string message)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                throw new InvalidOperationException(message);
            }
            return self;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [DebuggerNonUserCode]
        [return: NotNull]
        public static string ThrowIfNullOrEmpty(this string? self)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                throw new InvalidOperationException();
            }
            return self;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="parameterName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        [Obsolete(@"Use ThrowIf.ArgumentNullOrWhiteSpace instead.", true)]
        public static string ThrowIfArgumentNullOrWhiteSpace(this string self, string parameterName, string? message = null)
        {
            if (string.IsNullOrWhiteSpace(self))
            {

                throw message.IsNullOrWhiteSpace()
                    ? new ArgumentException($"{nameof(parameterName)} is required", nameof(parameterName))
                    : new ArgumentException(message, nameof(parameterName));
            }
            return self;
        }

        /// <summary>
        /// Converts this string to <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"> self or encoding </exception>
        [DebuggerNonUserCode]
        public static MemoryStream ToMemoryStream(this string self, Encoding encoding)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            return new MemoryStream(self.ToBytes(encoding));
        }


        /// <summary>
        /// Indicates whether a specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="self">The string to test.</param>
        /// <returns>
        ///   <c>true</c> if the value parameter is null or Empty, or if value consists exclusively of white-space characters.
        /// </returns>
        public static bool IsNullOrWhiteSpace(this string? self) => string.IsNullOrWhiteSpace(self);

        /// <summary>
        /// Defaults if null or white space.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">defaultValue</exception>
        [return: NotNull]
        public static string DefaultIfNullOrWhiteSpace(this string? self, string defaultValue)
        {
            return self.IsNullOrWhiteSpace()
                ? defaultValue
                : self!;
        }


        /// <summary>
        /// Returns a default value if the input string is null, empty, or consists only of whitespace characters.
        /// </summary>
        /// <param name="self">The input string to check for null or whitespace.</param>
        /// <param name="defaultValue">A function that returns the default value to use if the input string is null or whitespace.</param>
        /// <returns>Returns the input string if it is not null or whitespace; otherwise, returns the default value.</returns>
        /// <remarks>
        /// This method is an extension method that can be called on any string object. If the input string is null, empty, or consists only of whitespace characters,
        /// the method invokes the specified default value function to generate a default value to return instead. The method uses the <see cref="string.IsNullOrWhiteSpace(string)"/> method
        /// to check if the input string is null, empty, or consists only of whitespace characters.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Thrown if the input string is null and no default value function is provided.</exception>
        /// <returns>Returns a non-null string value.</returns>
        [return: NotNull]
        public static string DefaultIfNullOrWhiteSpace(this string? self, Func<string> defaultValue)
        {
            return self.IsNullOrWhiteSpace()
                ? defaultValue.Invoke()
                : self!;
        }


        /// <summary>
        /// Defaults if null or empty.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">defaultValue</exception>
        public static string DefaultIfNullOrEmpty(this string self, string defaultValue)
        {
            if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));
            return self.IsNullOrEmpty()
                ? defaultValue
                : self;
        }

        /// <summary>
        /// Indicates whether the specified string is null or an empty string ("").
        /// </summary>
        /// <param name="self">The string to test.</param>
        /// <returns>
        ///   <c>true</c> if the value parameter is null or an empty string (""); otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);

        /// <summary>
        /// Concatenates all the elements of this string array, using the specified separator between each element.
        /// </summary>
        /// <param name="self">An array that contains the elements to concatenate.</param>
        /// <param name="separator">The string to use as a separator. separator is included in the returned string only if value has more than one element.</param>
        /// <returns>A string that consists of the elements in value delimited by the separator string.
        /// -or- Empty if values has zero elements.</returns>
        /// <exception cref="ArgumentNullException">self</exception>
        [DebuggerNonUserCode]
        public static string Join(this IEnumerable<string> self, string separator = ",")
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            separator ??= ",";
            return string.Join(separator, self);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent replacement pattern.
        /// </summary>
        /// <param name="self">The input string.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="evaluator">The evaluator.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"> self or pattern or evaluator </exception>
        public static string Replace(this string self, string pattern, MatchEvaluator evaluator)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            return Regex.Replace(self, pattern, evaluator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="regex"></param>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Replace(this string self, Regex regex, MatchEvaluator evaluator)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (regex == null) throw new ArgumentNullException(nameof(regex));
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            return regex.Replace(self, evaluator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="regex"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Replace(this string self, Regex regex, string replacement)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (regex == null) throw new ArgumentNullException(nameof(regex));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            return regex.Replace(self, replacement);
        }


        /// <summary>
        /// Quotes this string according to the specified quote type.
        /// </summary>
        /// <param name="self">The string to quote.</param>
        /// <param name="quoteType">Type of the quote.</param>
        /// <returns>A quoted string</returns>
        public static string Quote(this string self, QuoteType quoteType = QuoteType.Double)
        {
            switch (quoteType)
            {
                case QuoteType.Double:
                    if (self.StartsWith("\"") && self.EndsWith("\"")) return self;
                    return $"\"{self}\"";
                case QuoteType.Single:
                    if (self.StartsWith("'") && self.EndsWith("'")) return self;
                    return $"'{self}'";
                case QuoteType.SqlLiteral:
                    if (self.StartsWith('\'') && self.EndsWith('\''))
                    {
                        self = self.Substring(1, self.Length - 2);
                    }
                    self = Regex.Replace(self, @"'+", match => match.Length % 2 == 0
                        ? match.Value
                        : match.Value + "'");
                    return $"'{self}'";
                case QuoteType.SqlIdentity:
                    if (self.StartsWith("\"") && self.EndsWith("\""))
                    {
                        self = self.Substring(1, self.Length - 2);
                    }
                    self = Regex.Replace(self, "\"+", match => match.Length % 2 == 0
                        ? match.Value
                        : match.Value + "\"");
                    return $"\"{self}\"";
                default:
                    throw new NotSupportedException($"{typeof(QuoteType)}.{quoteType} is not supported");
            }
        }

        /// <summary>
        /// Encodes all the characters in this string into a sequence of bytes using <see cref="Encoding.UTF8"/>.
        /// </summary>
        /// <param name="self">The string to be encoded.</param>
        /// <returns>A byte array containing the results of encoding the input string.</returns>
        /// <exception cref="ArgumentNullException">The string to be encoded</exception>
        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static byte[] ToUtf8Bytes(this string self)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            return Encoding.UTF8.GetBytes(self);
        }

        /// <summary>
        /// Encodes all the characters in this string into a sequence of bytes using the given <paramref name="encoding"/>
        /// </summary>
        /// <param name="self">The string to be encoded.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A byte array containing the results of encoding the input string.</returns>
        /// <exception cref="ArgumentNullException">self or encoding </exception>
        [DebuggerNonUserCode]
        public static byte[] ToBytes(this string self, Encoding encoding)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            return encoding.GetBytes(self);
        }

        /// <summary>
        /// Converts this string to its equivalent base 64 representation encoded using the given <paramref name="encoding"/>.
        /// </summary>
        /// <param name="self">The input string.</param>
        /// <param name="encoding">The character encoding.</param>
        /// <returns>The base 64 string representation of the input string.</returns>
        /// <exception cref="ArgumentNullException">self or encoding </exception>
        public static string ToBase64(this string self, Encoding encoding)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            return System.Convert.ToBase64String(self.ToBytes(encoding));
        }

    }
}
