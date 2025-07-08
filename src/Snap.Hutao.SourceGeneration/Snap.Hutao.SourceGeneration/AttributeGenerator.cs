// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration;

[Generator(LanguageNames.CSharp)]
internal sealed class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAllAttributes);
    }

    public static void GenerateAllAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        /* lang=c# */
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
                public bool InitializeComponent { get; set; }
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
            """;
        context.AddSource("Snap.Hutao.Core.Annotation.Attributes.g.cs", coreAnnotations);

        /* lang=c# */
        string coreDependencyInjectionAnnotationHttpClients = """
            using JetBrains.Annotations;
            
            namespace Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient;

            [MeansImplicitUse]
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

                /// <summary>
                /// HoyoPlay 登录请求配置
                /// </summary>
                XRpc6,
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

        /* lang=c# */
        string coreDependencyInjectionAnnotations = """
            using JetBrains.Annotations;
            
            namespace Snap.Hutao.Core.DependencyInjection.Annotation;

            internal enum InjectAs
            {
                Singleton,
                Transient,
                Scoped,
                HostedService,
            }

            [MeansImplicitUse]
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

        /* lang=c# */
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

        /* lang=c# */
        string interceptsLocation = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
            internal sealed class InterceptsLocationAttribute : Attribute
            {
                public InterceptsLocationAttribute(int version, string data)
                {
                }
            }
            """;
        context.AddSource("System.Runtime.CompilerServices.InterceptsLocationAttribute.g.cs", interceptsLocation);

        /* lang=c# */
        string fieldAccess = """
            namespace Snap.Hutao.Core.Annotation;
            
            [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            internal sealed class FieldAccessAttribute : Attribute
            {
                public FieldAccessAttribute()
                {
                }
            }
            """;
        context.AddSource("Snap.Hutao.Core.Annotation.FieldAccessAttribute.g.cs", fieldAccess);
    }
}