namespace SubmitService.Infrastructure;

public static class StringExtensions
{   
    /// <summary>
    /// 获取字符串的前15个字符
    /// <para>不足15个字符则有多少人取多少</para>
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string GetFirst15Wrods(this string input)
    {
        if (input.Length > 15)
        {
            return input.Substring(0, 15);
        }
        else
        {
            return input;
        }
    }
}
