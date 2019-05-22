using CloudPad.Internal;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CloudPad.FunctionApp {
  class FunctionExecutor : IFunctionMetadata {
    static FunctionExecutor() {
      System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(LINQPad.Util).TypeHandle);
    }

    private static Dictionary<string, TaskCompletionSource<FunctionExecutor>> _d = new Dictionary<string, TaskCompletionSource<FunctionExecutor>>(StringComparer.OrdinalIgnoreCase);

    public static async Task<FunctionExecutor> GetAndInvalidateAsync(string functionDir, TraceWriter log) {
      var functionJsonFileName = Path.Combine(functionDir, "function.json");

      var func = await GetAsync(functionDir, functionJsonFileName, log);

      // Invalidate file dependency
      // (this isn't perfect but it only needs to work for the 1 person debugging/testing the app)

      var functionJsonLastWriteTime = File.GetLastWriteTimeUtc(functionJsonFileName);
      if (func.LastWriteTime < functionJsonLastWriteTime) {
        lock (_d) {
          _d.Remove(functionJsonFileName);
        }
        return await GetAsync(functionDir, functionJsonFileName, log);
      }

      return func;
    }

    private static Task<FunctionExecutor> GetAsync(string functionDir, string functionJsonFileName, TraceWriter log) {
      TaskCompletionSource<FunctionExecutor> tcs;

      lock (_d) {
        if (_d.TryGetValue(functionJsonFileName, out tcs)) {
          return tcs.Task;
        }
        _d.Add(functionJsonFileName, tcs = new TaskCompletionSource<FunctionExecutor>());
      }

      try {
        var functionJsonLastWriteTime = File.GetLastWriteTimeUtc(functionJsonFileName);
        var functionJson = JObject.Parse(File.ReadAllText(functionJsonFileName));

        var metadata = functionJson["cloudPad"];

        var applicationBase = (string)metadata["applicationBase"];
        var scriptFile = (string)metadata["scriptFile"];
        var typeName = (string)metadata["typeName"];
        var methodName = (string)metadata["methodName"];
        var providerName = (string)metadata["providerName"];
        var connectionString = (string)metadata["connectionString"];

        log.Info($"InitializingFunction ApplicationBase='{applicationBase}', ScriptFile='{scriptFile}'", nameof(FunctionExecutor));

        var fullPath = Path.GetFullPath(Path.Combine(functionDir, applicationBase, scriptFile));
        var dir = Path.GetDirectoryName(fullPath);

        log.Info($"ScriptFileLoad '{fullPath}'", nameof(FunctionExecutor));

        var assembly = Assembly.LoadFrom(fullPath);
        var type = assembly.GetType(typeName);
        var method = type.GetMethod(methodName);
        var func = FunctionBinder.Bind(method);

        // launch cleanup task

        var f = new FunctionExecutor(func, functionJsonLastWriteTime, dir, providerName: providerName, connectionString: connectionString);

        tcs.SetResult(f);
      } catch (Exception ex) {
        tcs.SetException(ex);
      }

      return tcs.Task;
    }

    // ===

    public FunctionDescriptor Function { get; }
    public DateTime LastWriteTime { get; }
    public string ApplicationBase { get; }

    public string ProviderName { get; }
    public string ConnectionString { get; set; }

    public FunctionExecutor(FunctionDescriptor function, DateTime lastWriteTime, string applicationBase, string providerName, string connectionString) {
      Function = function;
      LastWriteTime = lastWriteTime;
      ApplicationBase = applicationBase;
      ProviderName = providerName;
      ConnectionString = connectionString;
    }

    public async Task<object> InvokeAsync(FunctionArgumentList arguments, TraceWriter log) {
      var m = Function.Method;

      using (var scope = Function.Activator.CreateScope(this)) {
        object userQuery;

        using (new LoaderLock(ApplicationBase)) {
          // the user query constructor must run with a loader like lock
          // this is because there's no other way for us to inject context
          // in a reliable manner

          userQuery = scope.CreateInstance(); // pass application base?
        }

        var result = m.Invoke(userQuery, arguments.Apply(Function.ParameterBindings));

        var task = result as Task;
        if (task != null) {
          await task; // do not exit scope until task is finished
        }

        return result;
      }
    }
  }
}