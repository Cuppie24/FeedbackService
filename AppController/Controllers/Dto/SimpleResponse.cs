namespace AppController.Controllers.Dto;

public class SimpleResponse<T>(T value, string message = "")
{
    public T Value { get; set; }
    public string Message { get; set; } = string.Empty;
}