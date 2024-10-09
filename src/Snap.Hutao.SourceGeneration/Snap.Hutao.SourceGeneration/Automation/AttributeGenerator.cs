// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAllAttributes);
    }

    public static void GenerateAllAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        /* lang=c#-test */
        string coreAnnotations = """
            using System.Diagnostics;

            namespace Snap.Hutao.Core.Annotation;

            [AttributeUsage(AttributeTargets.Method, Inherited = false)]
            internal sealed class CommandAttribute : Attribute
            {
                public CommandAttribute(string commandName)
                {
                }

                public CommandAttribute(string commandName, string canExecuteName)
                {
                }

                public bool AllowConcurrentExecutions { get; set; }
            }

            [AttributeUsage(AttributeTargets.Class, Inherited = false)]
            internal sealed class ConstructorGeneratedAttribute : Attribute
            {
                public ConstructorGeneratedAttribute()
                {
                }

                public bool CallBaseConstructor { get; set; }
                public bool ResolveHttpClient { get; set; }
            }

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
            internal sealed class DependencyPropertyAttribute : Attribute
            {
                public DependencyPropertyAttribute(string name, Type type)
                {
                }

                public DependencyPropertyAttribute(string name, Type type, object defaultValue)
                {
                }

                public DependencyPropertyAttribute(string name, Type type, object defaultValue, string valueChangedCallbackName)
                {
                }

                public bool IsAttached { get; set; }
                public Type AttachedType { get; set; } = default;
                public string RawDefaultValue { get; set; } = default;
            }

            [AttributeUsage(AttributeTargets.All, Inherited = false)]
            [Conditional("DEBUG")]
            internal sealed class HighQualityAttribute : Attribute
            {
            }
            """;
        context.AddSource("Snap.Hutao.Core.Annotation.Attributes.g.cs", coreAnnotations);

        /* lang=c#-test */
        string coreDependencyInjectionAnnotationHttpClients = """
            namespace Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient;

            [AttributeUsage(AttributeTargets.Class, Inherited = false)]
            internal sealed class HttpClientAttribute : Attribute
            {
                public HttpClientAttribute(HttpClientConfiguration configuration)
                {
                }

                public HttpClientAttribute(HttpClientConfiguration configuration, Type interfaceType)
                {
                }
            }

            internal enum HttpClientConfiguration
            {
                /// <summary>
                /// 默认配置
                /// </summary>
                Default,

                /// <summary>
                /// 米游社请求配置
                /// </summary>
                XRpc,

                /// <summary>
                /// 米游社登录请求配置
                /// </summary>
                XRpc2,

                /// <summary>
                /// Hoyolab app
                /// </summary>
                XRpc3,

                /// <summary>
                /// 米哈游启动器登录请求配置
                /// </summary>
                XRpc5,
            }

            [AttributeUsage(AttributeTargets.Class, Inherited = false)]
            internal sealed class PrimaryHttpMessageHandlerAttribute : Attribute
            {
                /// <inheritdoc cref="System.Net.Http.HttpClientHandler.MaxConnectionsPerServer"/>
                public int MaxConnectionsPerServer { get; set; }

                /// <summary>
                /// <inheritdoc cref="System.Net.Http.HttpClientHandler.UseCookies"/>
                /// </summary>
                public bool UseCookies { get; set; }
            }
            """;
        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.Attributes.g.cs", coreDependencyInjectionAnnotationHttpClients);

        /* lang=c#-test */
        string coreDependencyInjectionAnnotations = """
            namespace Snap.Hutao.Core.DependencyInjection.Annotation;

            internal enum InjectAs
            {
                Singleton,
                Transient,
                Scoped,
                HostedService,
            }

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
            internal sealed class InjectionAttribute : Attribute
            {
                public InjectionAttribute(InjectAs injectAs)
                {
                }

                public InjectionAttribute(InjectAs injectAs, Type interfaceType)
                {
                }

                public object Key { get; set; }
            }

            [AttributeUsage(AttributeTargets.Field)]
            internal sealed class FromKeyedServicesAttribute : Attribute
            {
                public FromKeyedServicesAttribute(object key)
                {
                }
            }
            """;
        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.Attributes.g.cs", coreDependencyInjectionAnnotations);

        /* lang=c#-test */
        string resourceLocalization = """
            namespace Snap.Hutao.Resource.Localization;

            [AttributeUsage(AttributeTargets.Enum)]
            internal sealed class LocalizationAttribute : Attribute
            {
            }

            [AttributeUsage(AttributeTargets.Field)]
            internal sealed class LocalizationKeyAttribute : Attribute
            {
                public LocalizationKeyAttribute(string key)
                {
                }
            }
            """;
        context.AddSource("Snap.Hutao.Resource.Localization.Attributes.g.cs", resourceLocalization);
    }
}
