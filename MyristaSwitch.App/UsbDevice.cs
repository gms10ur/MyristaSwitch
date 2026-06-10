namespace MyristaSwitch.App;

internal sealed record UsbDevice(
    string ClassName,
    string FriendlyName,
    string InstanceId,
    string Status,
    uint? ProblemCode,
    bool Present)
{
    public bool IsKeyboard => ClassName.Equals("Keyboard", StringComparison.OrdinalIgnoreCase);

    public bool IsMouseLike =>
        ClassName.Equals("Mouse", StringComparison.OrdinalIgnoreCase) ||
        ClassName.Equals("HIDClass", StringComparison.OrdinalIgnoreCase);

    public string DisplayName => string.IsNullOrWhiteSpace(FriendlyName)
        ? InstanceId
        : $"{FriendlyName} [{ClassName}]";

    public string Signature => $"{ClassName}|{FriendlyName}".ToUpperInvariant();

    public bool IsUsable => Present && (string.IsNullOrWhiteSpace(Status) || Status.Equals("OK", StringComparison.OrdinalIgnoreCase));
}
