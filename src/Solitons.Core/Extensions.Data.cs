using System;
using System.Data.Common;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading;

namespace Solitons;

public static partial class Extensions
{
    public static IObservable<Unit> Open(
        this DbConnection connection,
        CancellationToken cancellation) =>
        connection
            .OpenAsync(cancellation)
            .ToObservable();
}