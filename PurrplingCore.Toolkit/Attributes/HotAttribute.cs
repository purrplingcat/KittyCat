namespace PurrplingCore.Toolkit.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class HotAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public class ColdAttribute : Attribute { }
