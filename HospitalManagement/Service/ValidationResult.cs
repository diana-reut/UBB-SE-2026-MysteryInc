using System.Collections.Generic;

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new List<string>();

    public void AddError(string message)
    {
        IsValid = false;
        Errors.Add(message);
    }
}