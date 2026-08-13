namespace Figma2Unity
{
    /// <summary>
    /// Semantic node tag driving Drawer dispatch. Multiple tags may apply to a single node.
    /// </summary>
    public enum FcuTag
    {
        None = 0,
        Container = 1,
        Frame = 2,
        Page = 3,

        // Layout
        AutoLayoutGroup = 100,
        ContentSizeFitter = 101,

        // Content
        Text = 200,
        Image = 201,
        Slice9 = 204,
        AutoSlice9 = 205,

        // Interaction
        Button = 300,
        InputField = 301,
        Placeholder = 302,
        ScrollView = 303,
        PasswordField = 304,
        Toggle = 305,

        // Effects
        Shadow = 500,
        CanvasGroup = 501,
        Mask = 502,

        // Special
        Ignore = 600,
        Screen = 700,
        FlowSection = 701,
    }
}
