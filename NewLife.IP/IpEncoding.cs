using System.Text;

namespace NewLife.IP;

/// <summary>IP数据库文本编码</summary>
/// <remarks>
/// 纯真兼容库传统使用 GB2312/GBK；GB18030 是其超集，既能正确读取既有数据，
/// 又能在 DB-IP CSV 转换时保留中文以外的地名（带音标拉丁字母、阿拉伯文、韩文等）。
/// .NET Core 默认不带代码页编码，首次使用本类型时自动注册 <c>CodePagesEncodingProvider</c>。
/// </remarks>
internal static class IpEncoding
{
    /// <summary>数据库文本编码。GB18030，兼容 GB2312/GBK</summary>
    public static Encoding Db { get; } = Create();

    private static Encoding Create()
    {
#if NETCOREAPP
        // .NET Core/Android/iOS 默认只带 Unicode 编码，GB18030 需先注册代码页提供程序
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        try
        {
            return Encoding.GetEncoding("GB18030");
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
        {
            // 极端裁剪环境缺少 GB18030 时退回 GB2312（.NET 中映射为 GBK），保证既有纯真库仍可读取
            return Encoding.GetEncoding("GB2312");
        }
    }
}
