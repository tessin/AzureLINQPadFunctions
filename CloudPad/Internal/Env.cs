using System;

namespace Tessin.Internal
{
  internal static class Env
  {
    public static bool IsCompiling => Get("TESSIN_CLOUDPAD_ENV") == "compiling";

    public static bool IsHost => Get("TESSIN_CLOUDPAD_ENV") == "host";

    public static string Get(string name)
    {
      return Environment.GetEnvironmentVariable(name);
    }
  }
}