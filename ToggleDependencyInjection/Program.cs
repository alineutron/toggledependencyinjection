using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinFu.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace ToggleDependencyInjection
{
    class Program
    {
       
        static void Main(string[] args)
        {
            Console.WriteLine("Starting the app");
            var services = new ServiceCollection();
            services.AddSingleton<OnService>();
            services.AddSingleton<OffService>();
            services.RegisterCapabilityComponent<OnService, OffService, IService>();
            var serviceProvider = services.BuildServiceProvider();
            Console.WriteLine("Now setting up dependency injection --- DONE");
            var customService = serviceProvider.GetService<IService>();
            Console.WriteLine("Getting the service that we added --- DONE");
            Console.WriteLine("");
            var input = "";
            do
            {
                Console.WriteLine("Accessing the service to see what type of service we get back");
                Console.WriteLine("");
                customService.Serve();
                Console.WriteLine("----change the toggle and press y----");
                input = Console.ReadLine();
            } while (input.Equals("y"));


        }
    }


    public interface IService
    {
        void Serve();
    }


    public class OnService : IService
    {
        public void Serve()
        {
            Console.WriteLine("I am 'on' service");
            Console.ReadLine();
        }
    }

    public class OffService : IService
    {
        public void Serve()
        {
            Console.WriteLine("I am 'off' service");
            Console.ReadLine();
        }
    }


    public static class ServiceCollectionExtension
    {
        private static readonly ProxyFactory _proxyFactory = new ProxyFactory();

        public static IServiceCollection RegisterCapabilityComponent<TCapabilityOn, TCapabilityOff, TInterface>(
            this IServiceCollection services)
            where TCapabilityOn : TInterface
            where TCapabilityOff : TInterface
            where TInterface : class
        {
            if (!typeof(TInterface).IsInterface)
                throw new ArgumentException("TInterface type must be an interface. Capability class proxies not supported ATM.");
            return services.AddSingleton(c => _proxyFactory.CreateProxy<TInterface>(new CapabilityTypeInterceptor<TCapabilityOn, TCapabilityOff>(services)));
        }

        private class CapabilityTypeInterceptor<TCapabilityOn, TCapabilityOff> : IInterceptor
        {
           
            private readonly TCapabilityOn _onService;
            private readonly TCapabilityOff _offService;

            public CapabilityTypeInterceptor(IServiceCollection serviceProvider)
            {
                var sp = serviceProvider.BuildServiceProvider();
                _onService = sp.GetService<TCapabilityOn>();
                _offService = sp.GetService<TCapabilityOff>();
            }

            public object Intercept(InvocationInfo info)
            {
                var content = string.Empty;
                using (var text = File.OpenText("toggles.txt"))
                {
                    content = text.ReadToEnd();
                }
                
                var svc = new object();
                if (content.Equals("OFF")) svc = _offService;
                else svc = _onService;
                return info.TargetMethod.Invoke(svc, info.Arguments);
            }

        }
    }

}
