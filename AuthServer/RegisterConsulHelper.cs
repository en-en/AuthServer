using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace IdentityServer4.Configuration
{
    public static class RegisterConsulHelper
    {
        public static IApplicationBuilder RegisterConsul(this IApplicationBuilder app, IApplicationLifetime lifetime,
            ServiceEntity entity)
        {
            //请求注册的consul地址
            var consulClient=new ConsulClient(x=>x.Address=new Uri($"http://{entity.ConsulIP}:{entity.ConsulPort}"));

            var httpCheck=new AgentServiceCheck()
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                Interval = TimeSpan.FromSeconds(10),
                HTTP = $"Http://{entity.IP}:{entity.Port}/api/health/index",
                Timeout = TimeSpan.FromSeconds(5)

            };
            //注册到consul
            var reagistration = new AgentServiceRegistration()
            {
                Checks = new[] {httpCheck},
                ID = Guid.NewGuid().ToString(),
                Name = entity.ServiceName,
                Address = entity.IP,
                Port = entity.Port,
                Tags = new[] {$"urlprefix-/{entity.ServiceName}"}
            };
            //服务启动时注册，内部实际由consul api注册 httpClient发起
            consulClient.Agent.ServiceRegister(reagistration).Wait();
          
            lifetime.ApplicationStopped.Register(() =>
            {
                consulClient.Agent.ServiceDeregister(reagistration.ID).Wait();
            });
            return app;
        }
    }

    public class ServiceEntity
    {
        public string ConsulIP { get; set; }

        public int ConsulPort { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public string ServiceName { get; set; }
    }
}
