using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Wavefront.AspNetCore.SDK.CSharp.Common;
using Wavefront.OpenTracing.SDK.CSharp;
using Wavefront.OpenTracing.SDK.CSharp.Reporting;
using Wavefront.SDK.CSharp.Common.Application;
using Wavefront.SDK.CSharp.Proxy;

using OpenTracing;

namespace wavefront_sdk
{
   public static class SteeltoeWavefrontProxyExtensions
   {
      public class WavefrontProxyOptions
      {
            public WavefrontProxyOptions()
            {
               Port = 2878;
               DistributionPort = 2878;
               TracingPort = 30000;
               ReportingIntervalSeconds = 30;
               FlushIntervalSeconds = 2;
            }

            public const string WavefrontProxy = "wavefront-proxy";
            public string Hostname { get; set; }
            public int Port { get; set; }
            public int DistributionPort { get; set; }
            public int TracingPort { get; set; }
            public string Application { get; set; }
            public string Service { get; set; }
            public string Cluster { get; set; }
            public string Shard { get; set; }
            public string Source {get; set;}
            public int ReportingIntervalSeconds {get; set;}
            public int FlushIntervalSeconds { get; set; }
      }

      public static IServiceCollection AddWavefrontProxy(this IServiceCollection services, IConfiguration configuration)
      {
            var waveFrontProxyConfiguration = 
               configuration.GetSection(WavefrontProxyOptions.WavefrontProxy).Get<WavefrontProxyOptions>();

            var wfProxyClientBuilder = new WavefrontProxyClient.Builder(waveFrontProxyConfiguration.Hostname);
            wfProxyClientBuilder.MetricsPort(waveFrontProxyConfiguration.Port);
            wfProxyClientBuilder.DistributionPort(waveFrontProxyConfiguration.DistributionPort);
            wfProxyClientBuilder.TracingPort(waveFrontProxyConfiguration.TracingPort);
            wfProxyClientBuilder.FlushIntervalSeconds(waveFrontProxyConfiguration.TracingPort);
            var wavefrontSender = wfProxyClientBuilder.Build();

            var applicationTags = new ApplicationTags.Builder(waveFrontProxyConfiguration.Application, waveFrontProxyConfiguration.Service)
            .Cluster(waveFrontProxyConfiguration.Cluster)
            .Shard(waveFrontProxyConfiguration.Shard)
            .Build();

            var wfAspNetCoreReporter = new WavefrontAspNetCoreReporter.Builder(applicationTags)
               .WithSource(waveFrontProxyConfiguration.Source)
               .ReportingIntervalSeconds(waveFrontProxyConfiguration.ReportingIntervalSeconds)
               .Build(wavefrontSender);

            System.Console.WriteLine(wfAspNetCoreReporter);

            var wavefrontSpanReporter = new WavefrontSpanReporter.Builder()
            .Build(wavefrontSender);

            ITracer tracer = new WavefrontTracer.Builder(wavefrontSpanReporter, applicationTags).Build();

            services.AddWavefrontForMvc(wfAspNetCoreReporter, tracer);

            return services;
      }
   }
}