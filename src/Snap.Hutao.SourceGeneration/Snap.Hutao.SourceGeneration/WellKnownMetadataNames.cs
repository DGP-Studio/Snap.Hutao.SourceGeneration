﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.SourceGeneration;

internal sealed class WellKnownMetadataNames
{
    public const string CommandAttribute = "Snap.Hutao.Core.Annotation.CommandAttribute";
    public const string ConstructorGeneratedAttribute = "Snap.Hutao.Core.Annotation.ConstructorGeneratedAttribute";
    public const string DependencyPropertyAttribute = "Snap.Hutao.Core.Annotation.DependencyPropertyAttribute";
    public const string DependencyPropertyAttributeT = "Snap.Hutao.Core.Annotation.DependencyPropertyAttribute`1";
    public const string FieldAccessAttribute = "Snap.Hutao.Core.Annotation.FieldAccessorAttribute";

    public const string HttpClientAttribute = "Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.HttpClientAttribute";
    public const string PrimaryHttpMessageHandlerAttribute = "Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.PrimaryHttpMessageHandlerAttribute";
    public const string HttpClientConfiguration = "Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.HttpClientConfiguration.";

    public const string ServiceLifetimeSingleton = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton";
    public const string ServiceLifetimeScoped = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped";
    public const string ServiceLifetimeTransient = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";

    public const string ServiceAttribute = "Snap.Hutao.Core.DependencyInjection.Annotation.ServiceAttribute";
    public const string FromKeyedServicesAttribute = "Snap.Hutao.Core.DependencyInjection.Annotation.FromKeyedServicesAttribute";

    public const string ExtendedEnumAttribute = "Snap.Hutao.Resource.Localization.ExtendedEnumAttribute";
    public const string LocalizationKeyAttribute = "Snap.Hutao.Resource.Localization.LocalizationKeyAttribute";
    public const string InterceptsLocationAttribute = "System.Runtime.CompilerServices.InterceptsLocationAttribute";
}