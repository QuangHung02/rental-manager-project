namespace RentalManager.DTOs;

public class EnumOption<T>
{
    public EnumOption(T value, string text)
    {
        Value = value;
        Text = text;
    }

    public T Value { get; }
    public string Text { get; }
}
