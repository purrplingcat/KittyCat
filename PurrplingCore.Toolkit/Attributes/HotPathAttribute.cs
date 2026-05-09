namespace PurrplingCore.Toolkit.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class HotPathAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public class ColdPathAttribute : Attribute { }
