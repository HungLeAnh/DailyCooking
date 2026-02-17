using System.Text.RegularExpressions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    public static bool ValidateInput(string inputText,int maxLength = 20)
    {
        if (inputText.Length > maxLength)
        {
            //Debug.LogWarning("Too long Text: " + maxLength);
            return false;
        }

        string pattern = @"^[a-zA-Z0-9 ]*$";

        if (!Regex.IsMatch(inputText, pattern))
        {
            //Debug.LogError("Invalid character!");
            return false;
        }
        else
        {
            //Debug.Log("valid input " + inputText);
            return true;
        }
    }
}