// Copyright (c) Arash Khatami
// Distributed under the MIT license. See the LICENSE file in the project root for more information.
struct ShaderConstants
{
    uint GPassMainBufferIndex;
};

ConstantBuffer<ShaderConstants> ShaderParams : register(b1);
Texture2D Textures[] : register(t0, space0);

float4 PostProcessPS(in noperspective float4 Position : SV_Position,
                     in noperspective float2 UV : TEXCOORD) : SV_Target0
{
    Texture2D gpassMain = Textures[ShaderParams.GPassMainBufferIndex];
    return float4(gpassMain[Position.xy].xyz, 1.f);
}