using System.Collections.Generic;
using System;
using System.Reflection;

namespace Solitons.Reflection;

public abstract class MethodInfoDecorator(MethodInfo method)
{
    private readonly MethodInfo _method = method;

    /// <summary>
    /// Allows implicit conversion from <see cref="MethodInfoDecorator"/> to <see cref="MethodInfo"/>.
    /// </summary>
    /// <param name="decorator">The decorator to convert.</param>
    /// <returns>The underlying <see cref="MethodInfo"/> instance.</returns>
    public static implicit operator MethodInfo(MethodInfoDecorator decorator) => decorator._method;

    /// <inheritdoc/>
    public override string ToString() => _method.ToString();

    /// <inheritdoc/>
    public override bool Equals(object? obj) => _method.Equals(obj);

    /// <inheritdoc/>
    public override int GetHashCode() => _method.GetHashCode();

    // Delegating all public properties of MethodInfo to _method
    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    public virtual string Name => _method.Name;

    /// <summary>
    /// Gets the declaring type of the method.
    /// </summary>
    public virtual Type? DeclaringType => _method.DeclaringType;

    /// <summary>
    /// Gets the return type of the method.
    /// </summary>
    public virtual Type ReturnType => _method.ReturnType;

    /// <summary>
    /// Gets the attributes associated with the method.
    /// </summary>
    public virtual MethodAttributes Attributes => _method.Attributes;

    /// <summary>
    /// Gets a value indicating whether the method is static.
    /// </summary>
    public virtual bool IsStatic => _method.IsStatic;

    /// <summary>
    /// Gets a value indicating whether the method is public.
    /// </summary>
    public virtual bool IsPublic => _method.IsPublic;

    /// <summary>
    /// Gets a value indicating whether the method is abstract.
    /// </summary>
    public virtual bool IsAbstract => _method.IsAbstract;

    /// <summary>
    /// Gets a value indicating whether the method is virtual.
    /// </summary>
    public virtual bool IsVirtual => _method.IsVirtual;

    /// <summary>
    /// Gets the custom attributes applied to this method.
    /// </summary>
    public virtual IEnumerable<CustomAttributeData> CustomAttributes => _method.CustomAttributes;

    // Delegating all public methods of MethodInfo to _method
    /// <summary>
    /// Returns the parameters of the method.
    /// </summary>
    /// <returns>An array of <see cref="ParameterInfo"/> representing the parameters of the method.</returns>
    public virtual ParameterInfo[] GetParameters() => _method.GetParameters();

    /// <summary>
    /// Invokes the method on the given object.
    /// </summary>
    /// <param name="obj">The object on which to invoke the method.</param>
    /// <param name="parameters">An array of arguments to pass to the method.</param>
    /// <returns>The return value of the method, or <c>null</c> for methods that return void.</returns>
    public virtual object? Invoke(object? obj, object?[]? parameters) => _method.Invoke(obj, parameters);

    /// <summary>
    /// Gets the generic arguments of the method, if any.
    /// </summary>
    /// <returns>An array of <see cref="Type"/> objects representing the generic arguments.</returns>
    public virtual Type[] GetGenericArguments() => _method.GetGenericArguments();

    /// <summary>
    /// Determines whether a specific attribute is defined on the method.
    /// </summary>
    /// <param name="attributeType">The type of the attribute to search for.</param>
    /// <param name="inherit">Whether to search the inheritance chain.</param>
    /// <returns><c>true</c> if the attribute is defined; otherwise, <c>false</c>.</returns>
    public virtual bool IsDefined(Type attributeType, bool inherit) => _method.IsDefined(attributeType, inherit);

    /// <summary>
    /// Retrieves the custom attributes of the specified type applied to this method.
    /// </summary>
    /// <typeparam name="T">The type of attributes to retrieve.</typeparam>
    /// <param name="inherit">Whether to search the inheritance chain.</param>
    /// <returns>A collection of custom attributes of the specified type.</returns>
    public virtual IEnumerable<T> GetCustomAttributes<T>(bool inherit) where T : Attribute =>
        _method.GetCustomAttributes<T>(inherit);

    /// <summary>
    /// Retrieves all custom attributes applied to this method.
    /// </summary>
    /// <param name="inherit">Whether to search the inheritance chain.</param>
    /// <returns>A collection of custom attributes.</returns>
    public virtual object[] GetCustomAttributes(bool inherit) =>
        _method.GetCustomAttributes(inherit);

    /// <summary>
    /// Retrieves all custom attributes of the specified type applied to this method.
    /// </summary>
    /// <param name="attributeType">The type of attributes to retrieve.</param>
    /// <param name="inherit">Whether to search the inheritance chain.</param>
    /// <returns>A collection of custom attributes of the specified type.</returns>
    public virtual object[] GetCustomAttributes(Type attributeType, bool inherit) =>
        _method.GetCustomAttributes(attributeType, inherit);
}