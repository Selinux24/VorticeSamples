// Copyright (c) Arash Khatami
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

struct GlobalShaderData
{
    float4x4 View;
    float4x4 Projection;
    float4x4 InvProjection;
    float4x4 ViewProjection;
    float4x4 InvViewProjection;

    float3 CameraPosition;
    uint ViewWidth;

    float3 CameraDirection;
    uint ViewHeight;

    float DeltaTime;
};

struct PerObjectData
{
    float4x4 World;
    float4x4 InvWorld;
    float4x4 WorldViewProjection;
};

struct DirectionalLightParameters
{
    float3 Direction;
    float Intensity;

    float3 Color;
    float _pad;
};
