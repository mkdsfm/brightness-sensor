using System.Text;
using BrightnessSensor.App.Application;

namespace BrightnessSensor.App;

// Entry point: initializes console encoding and starts the Windows app loop.
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        return BrightnessApplication.Run(args);
    }
}
