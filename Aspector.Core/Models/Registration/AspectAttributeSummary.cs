﻿using Aspector.Core.Attributes;
using System.Reflection;

namespace Aspector.Core.Models.Registration
{
    public class AspectAttributeSummary
    {
        public AspectAttributeSummary((MethodInfo Method, AspectAttribute[] Aspects)[] inputs)
        {
            var maxDepthByMethod = new Dictionary<MethodInfo, int>();
            foreach (var input in inputs)
            {
                Type? lastType = null;
                var (decoratedMethod, aspectAttributes) = input;
                var theseLayersFromInnermost = new List<AspectAttributeLayer>();
                LayersFromInnermostByMethod[decoratedMethod] = theseLayersFromInnermost;

                for (var d = aspectAttributes.Length - 1; d >= 0; d--)
                {
                    var targetAttribute = aspectAttributes[d];
                    var targetAttributeType = targetAttribute.GetType();
                    if (lastType != targetAttributeType)
                    {
                        // mark new layer
                        lastType = targetAttributeType;
                        var thisLayerIndex = 0;
                        theseLayersFromInnermost.Add(new AspectAttributeLayer(thisLayerIndex, targetAttribute));
                    }
                    else
                    {
                        theseLayersFromInnermost.Last().Add(targetAttribute);
                    }
                }

                maxDepthByMethod[decoratedMethod] = theseLayersFromInnermost.Count;
            }

            var maxDepth = maxDepthByMethod.Max(kvp => kvp.Value);
            var analysisStructure = inputs.Select(i => LayersFromInnermostByMethod[i.Method]).Where(col => col.Count > 0).ToList();

            var higherLevelLayerStack = new Stack<(Type AspectType, List<(int RowIndex, int ColumnIndex)> Coordinates)>();

            for (var rowIndex = 0; rowIndex < maxDepth; rowIndex++)
            {
                for (var methodColumnIndex = 0; methodColumnIndex < analysisStructure.Count; methodColumnIndex++)
                {
                    var currentMethodColumn = analysisStructure[methodColumnIndex];
                    var nextRowIndex = rowIndex + 1;
                    var previousRowIndex = rowIndex - 1;
                    var previousColumnIndex = methodColumnIndex - 1;
                    var nextColumnIndex = methodColumnIndex + 1;
                    var maxColumnIndex = analysisStructure.Count - 1;

                    if (rowIndex >= currentMethodColumn.Count)
                    {
                        // empty coordinate logic? any?
                        continue;
                    }

                    var currentAspect = currentMethodColumn[rowIndex];
                    var currentAspectCanBeAddedThisIteration = true;

                    // define layer reservation and checking logic
                    Action<int, int> checkAndUpdateColumnReservation = (columnToCheck, rowToCheck) =>
                    {
                        var previouslyReservedLayerExists = higherLevelLayerStack.TryPeek(out var priorityReservedLayer);
                        var matchedDiagonalIsReserved = priorityReservedLayer.Coordinates?.Any(c => c.ColumnIndex == columnToCheck) == true;
                        var previousReservedLayerIsCurrentType = priorityReservedLayer.AspectType == currentAspect.AspectType;

                        if (!previouslyReservedLayerExists || !matchedDiagonalIsReserved)
                        {
                            currentAspectCanBeAddedThisIteration = false;
                            // add to stack
                            var coordinatesToAdd = new List<(int RowIndex, int ColumnIndex)>
                            {
                                (rowIndex, methodColumnIndex)
                            };

                            //if (rowToCheck > rowIndex)
                            //{
                            //    coordinatesToAdd.Add((rowToCheck, columnToCheck));
                            //}

                            higherLevelLayerStack.Push((currentAspect.AspectType, coordinatesToAdd));
                        }
                        else
                        {
                            if (priorityReservedLayer.AspectType == currentAspect.AspectType)
                            {
                                priorityReservedLayer.Coordinates?.Add((RowIndex: rowIndex, ColumnIndex: methodColumnIndex));
                                if (methodColumnIndex < maxColumnIndex)
                                {
                                    currentAspectCanBeAddedThisIteration = false;
                                }
                            }
                        }
                    };

                    // check left diagonal/left hand side if applicable
                    if (methodColumnIndex > 0 
                        && analysisStructure[previousColumnIndex].Count > nextRowIndex
                        && (analysisStructure[previousColumnIndex][nextRowIndex].AspectType == currentAspect.AspectType 
                            || analysisStructure[previousColumnIndex][rowIndex].AspectType == currentAspect.AspectType))
                    {
                        var rowToCheck = analysisStructure[previousColumnIndex][nextRowIndex].AspectType == currentAspect.AspectType ?
                            nextRowIndex : rowIndex;
                        checkAndUpdateColumnReservation(previousColumnIndex, rowToCheck);
                    }

                    // check right diagonal if applicable
                    if (methodColumnIndex < analysisStructure.Count - 1
                        && analysisStructure[nextColumnIndex].Count > nextRowIndex
                        && (analysisStructure[nextColumnIndex][nextRowIndex].AspectType == currentAspect.AspectType
                            || analysisStructure[nextColumnIndex][rowIndex].AspectType == currentAspect.AspectType))
                    {
                        var rowToCheck = analysisStructure[nextColumnIndex][nextRowIndex].AspectType == currentAspect.AspectType ?
                            nextRowIndex : rowIndex;
                        checkAndUpdateColumnReservation(nextColumnIndex, rowToCheck);
                    }

                    if (currentAspectCanBeAddedThisIteration)
                    {
                        // now add to wrap order
                        var wrapLayerIndex = AddToWrapOrder(currentAspect.AspectType);

                        // final layer index in wrap order may differ to per-method analysis - update affected layers
                        var layersToUpdate = new List<AspectAttributeLayer> { analysisStructure[methodColumnIndex][rowIndex] };
                        if (higherLevelLayerStack.TryPeek(out var nextHigherLayer)
                            && nextHigherLayer.AspectType == currentAspect.AspectType
                            && nextHigherLayer.Coordinates.Any(c => c.ColumnIndex == methodColumnIndex && c.RowIndex == rowIndex))
                        {
                            layersToUpdate = higherLevelLayerStack.Pop().Coordinates.Select(c => analysisStructure[c.ColumnIndex][c.RowIndex]).ToList();
                        }

                        layersToUpdate.ForEach(layer => layer.LayerIndex = wrapLayerIndex);
                    }
                }
            }

            LayersByType = LayersFromInnermostByMethod.Aggregate(
                seed: new Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>>(),
                func: (layerDictionariesByMethod, singleMethod) =>
                {
                    if (!layerDictionariesByMethod.TryGetValue(singleMethod.Key, out var aspectTypeAsLayers))
                    {
                        aspectTypeAsLayers = singleMethod.Value.Aggregate(
                            seed: new Dictionary<Type, Dictionary<int, AspectAttributeLayer>>(),
                            func: (layersByAspectType, aspectLayer) =>
                            {
                                if (!layersByAspectType.TryGetValue(aspectLayer.AspectType, out var layersForThisType))
                                {
                                    layersForThisType = new Dictionary<int, AspectAttributeLayer>();
                                    layersByAspectType[aspectLayer.AspectType] = layersForThisType;
                                }

                                layersForThisType[aspectLayer.LayerIndex] = AspectAttributeLayer.FromReversed(aspectLayer);
                                return layersByAspectType;
                            });

                        layerDictionariesByMethod[singleMethod.Key] = aspectTypeAsLayers;
                    }

                    return layerDictionariesByMethod;
                });
        }

        private int AddToWrapOrder(Type type)
        {
            if (!MaximumIndexByType.TryGetValue(type, out var currentMaxIndex))
            {
                currentMaxIndex = -1;
            }
            MaximumIndexByType[type] = currentMaxIndex + 1;

            var newProxyLayerDescriptor = (AspectType: type, LayerIndex: MaximumIndexByType[type]);
            WrapOrderFromInnermost.Add(newProxyLayerDescriptor);

            return newProxyLayerDescriptor.LayerIndex;
        }

        public Dictionary<Type, int> MaximumIndexByType = new Dictionary<Type, int>();
        public List<(Type AspectType, int LayerIndex)> WrapOrderFromInnermost = new List<(Type AspectType, int LayerIndex)>();
        public Dictionary<MethodInfo, List<AspectAttributeLayer>> LayersFromInnermostByMethod { get; } = new Dictionary<MethodInfo, List<AspectAttributeLayer>>();
        public Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>> LayersByType { get; } = new Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>>();
    }
}
