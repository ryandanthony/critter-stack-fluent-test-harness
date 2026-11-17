// -----------------------------------------------------------------------
// <copyright file="PayloadFormatter.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using JasperFx.Events;
using Marten;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Writes events, responses and messages into failure messages as JSON, using the store's own serializer so the
/// output matches what is stored.
/// </summary>
internal static class PayloadFormatter
{
    /// <summary>
    /// Lists events as <c>v{version} {type} {json}</c>, one per line.
    /// </summary>
    /// <param name="context">The scenario context, for the serializer.</param>
    /// <param name="store">The store the events came from, whose serializer stored them.</param>
    /// <param name="events">The events.</param>
    /// <returns>The formatted list, or <c>(no events)</c>.</returns>
    internal static string FormatEvents(ScenarioContext context, StoreRef store, IReadOnlyList<IEvent> events)
    {
        if (events.Count == 0)
        {
            return "  (no events)";
        }

        var builder = new StringBuilder();
        foreach (var e in events)
        {
            builder.Append("  v").Append(e.Version).Append(' ')
                .Append(Failure.TypeName(e.Data.GetType())).Append(' ')
                .AppendLine(Json(context.StoreFor(store), e.Data));
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Lists messages as <c>{type} {json}</c>, one per line.
    /// </summary>
    /// <param name="context">The scenario context, for the serializer.</param>
    /// <param name="messages">The messages.</param>
    /// <returns>The formatted list, or <c>(no messages)</c>.</returns>
    internal static string FormatMessages(ScenarioContext context, IReadOnlyList<object> messages)
    {
        if (messages.Count == 0)
        {
            return "  (no messages)";
        }

        return string.Join(
            Environment.NewLine,
            messages.Select(m => $"  {Failure.TypeName(m.GetType())} {Json(context, m)}"));
    }

    /// <summary>
    /// Serializes a value with the main store's serializer.
    /// </summary>
    /// <param name="context">The scenario context, for the serializer.</param>
    /// <param name="value">The value.</param>
    /// <returns>The JSON, or <c>null</c>.</returns>
    internal static string Json(ScenarioContext context, object? value) => Json(context.Store, value);

    /// <summary>
    /// Serializes a value with <paramref name="store"/>'s serializer.
    /// </summary>
    /// <param name="store">The store whose serializer to use.</param>
    /// <param name="value">The value.</param>
    /// <returns>The JSON, or <c>null</c>.</returns>
    internal static string Json(IDocumentStore store, object? value)
    {
        if (value is null)
        {
            return "null";
        }

        try
        {
            return store.Options.Serializer().ToJson(value);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException)
        {
            return $"(could not serialize: {Failure.TypeName(ex.GetType())})";
        }
    }
}
