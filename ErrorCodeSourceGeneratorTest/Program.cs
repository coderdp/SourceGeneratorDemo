namespace ErrorCodeSourceGeneratorTest;

using System.Globalization;

using ErrorCodeSourceGeneratorTest.Errors;

internal class Program
{
    static void Main(string[] args)
    {
        ChangeCultureInfo(new CultureInfo("zh-Hans"));
        Console.WriteLine("--------------change cultureInfo--------------");

        ChangeCultureInfo(new CultureInfo("en-US"));
        Console.WriteLine("--------------change cultureInfo--------------");

        ChangeCultureInfo(new CultureInfo("zh-Hans"));

        Console.WriteLine("end");
    }

    private static void ChangeCultureInfo(CultureInfo cultureInfo)
    {
        // 切换
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        PrintError();
    }

    private static void PrintError()
    {
        var error1 = OrderErrors.Error_1.FormatWith("description");
        var error2 = OrderErrors.Error_2;
        PrintErrorCore(error1);
        PrintErrorCore(error2);
    }

    private static void PrintErrorCore(Error error)
    {
        Console.WriteLine($"{error.Code}:{error.Message}");
    }
}

