using CloudPad.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace CloudPad
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var linqPadScriptFileName = Path.GetFullPath(@"..\..\..\test_hello.linq");

            using (var invoker = new Invoker())
            {
                await invoker.RunTimerTriggerAsync(linqPadScriptFileName, "Hello");
            }
        }
    }
}
