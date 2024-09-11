using System;
using System.Reactive;
using Xunit;

namespace Solitons.CommandLine;

public class CliOptionInfo_GetOptionTypeDescriptor_Should
{
    [Fact]
    public void HandleStringOptions()
    {
        var descriptor = CliOptionInfo.GetOptionTypeDescriptor(typeof(string));
        var valueDescriptor = descriptor as CliValueOptionTypeDescriptor;
        Assert.NotNull(valueDescriptor);
        Assert.Equal(typeof(string), valueDescriptor.ValueType);
    }


    [Theory]
    [InlineData(typeof(Unit))]
    [InlineData(typeof(CliFlag))]
    public void HandleFlagOptions(Type flagType)
    {
        var descriptor = CliOptionInfo.GetOptionTypeDescriptor(flagType);
        var flagDescriptor = descriptor as CliFlagOptionTypeDescriptor;
        Assert.NotNull(flagDescriptor);
        Assert.Equal(flagType, flagDescriptor.FlagType);
    }
}