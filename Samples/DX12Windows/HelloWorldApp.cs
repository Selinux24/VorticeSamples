using Engine;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory factory) : Application(factory)
    {
        public static HelloWorldApp Start<T>() where T : IPlatformFactory, new()
        {
            return new HelloWorldApp(new T());
        }
    }
}
