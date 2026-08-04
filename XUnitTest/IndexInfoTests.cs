using System;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

/// <summary>索引结构测试</summary>
public class IndexInfoTests
{
    [Fact(DisplayName = "索引结构为值类型")]
    public void IsValueType()
    {
        Assert.True(typeof(IndexInfo).IsValueType);
    }

    [Fact(DisplayName = "索引结构字段可读写")]
    public void Fields()
    {
        var info = new IndexInfo
        {
            Start = 0x01020304u,
            End = 0x05060708u,
            Offset = 0x09101112u,
        };

        Assert.Equal(0x01020304u, info.Start);
        Assert.Equal(0x05060708u, info.End);
        Assert.Equal(0x09101112u, info.Offset);
    }
}
