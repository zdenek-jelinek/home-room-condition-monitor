using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rcm.Testing.Threading.Tasks;

public static class TaskExtensions
{
    public static async Task<bool> TryWait(this Task task, TimeSpan timeout)
    {
        return await Task.WhenAny(task, Task.Delay(timeout, TestContext.CurrentContext.CancellationToken)) == task;
    }
}
