﻿using System;
using System.Linq;
using OpenTS2.Files.Utils;
using UnityEngine;

namespace OpenTS2.Files.Formats.DBPF.Scenegraph.Block
{
    public class AnimResourceConstBlock : ScenegraphDataBlock
    {
        public const uint TYPE_ID = 0xfb00791e;
        public const string BLOCK_NAME = "cAnimResourceConst";
        public override string BlockName => BLOCK_NAME;

        public AnimResourceConstBlock(PersistTypeInfo blockTypeInfo) : base(blockTypeInfo)
        {
        }

        public class AnimTarget
        {
            public string TagName;

            public int NumSharedChannels;
            public SharedChannel[] Channels;

            public int NumIKChains;
            public IKChain[] IKChains;
        }

        /// <summary>
        /// Inverse kinematics chain.
        /// </summary>
        public class IKChain
        {
            public int NumIkTargets;
            public Target[] IkTargets;

            public class Target
            {

            }
        }

        public class SharedChannel
        {
            public string ChannelName;

            public uint BoneHash;
            public uint ChannelFlags;

            public ChannelComponent[] Components;
        }

        /// <summary>
        /// This is where the actual animation keyframes live. Each animation channel can have multiple of these.
        /// </summary>
        public class ChannelComponent
        {
            public DataType Type;
            public CurveType TangentCurveType;
            public int NumKeyFrames;
            public IKeyFrame[] KeyFrames;

            internal void ReadKeyFrames(IoBuffer reader)
            {
                KeyFrames = new IKeyFrame[NumKeyFrames];

                for (var i = 0; i < NumKeyFrames; i++)
                {
                    if (TangentCurveType == CurveType.BakedTangents)
                    {
                        var keyframe = new IKeyFrame.BakedKeyFrame();
                        KeyFrames[i] = keyframe;

                        if (Type == DataType.FloatingPoint32)
                        {
                            keyframe.Data = reader.ReadFloat();
                        }
                        else
                        {
                            // TODO: convert from fixed point to floating point here.
                            reader.ReadInt16();
                        }
                    } else if (TangentCurveType == CurveType.ContinuousTangents)
                    {
                        var keyframe = new IKeyFrame.ContinuousKeyFrame();
                        KeyFrames[i] = keyframe;

                        keyframe.Short1 = reader.ReadUInt16();

                        if (Type == DataType.FloatingPoint32)
                        {
                            keyframe.Data = reader.ReadFloat();
                        }
                        else
                        {
                            // TODO: convert from fixed point to floating point here.
                            reader.ReadUInt16();
                        }

                        keyframe.Short2 = reader.ReadInt16();
                    } else if (TangentCurveType == CurveType.DiscontinuousTangents)
                    {
                        var keyframe = new IKeyFrame.DiscontinuousKeyFrame();
                        KeyFrames[i] = keyframe;

                        keyframe.Short1 = reader.ReadUInt16();
                        if (Type == DataType.FloatingPoint32)
                        {
                            keyframe.Data = reader.ReadFloat();
                        }
                        else
                        {
                            // TODO: convert from fixed point to floating point here.
                            reader.ReadUInt16();
                        }

                        keyframe.Short2 = reader.ReadInt16();
                        keyframe.Short3 = reader.ReadInt16();
                    }
                }
            }


            public enum CurveType
            {
                BakedTangents = 0,
                ContinuousTangents = 1,
                DiscontinuousTangents = 2,
            }

            public static CurveType ChannelCurveTypeFromPackedByte(byte packedByte)
            {
                if (!Enum.IsDefined(typeof(CurveType), packedByte & 0b11))
                {
                    throw new ArgumentException($"Invalid curve type: {packedByte & 0b11}");
                }
                return (CurveType)(packedByte & 0b11);
            }

            public enum DataType
            {
                /// 8.7 fixed point
                FixedPoint8_7,
                /// 9.7 fixed point
                FixedPoint9_7,

                /// 5.10 Fixed point
                FixedPoint5_10,
                /// 5.11 fixed point
                FixedPoint5_11,

                /// 3.12 fixed point
                FixedPoint3_12,
                /// 3.13 fixed point
                FixedPoint3_13,

                /// plain floats.
                FloatingPoint32,
            }

            public static DataType ChannelDataTypeFromSerializedPackedByte(byte packedByte) {
                // 5th bit set is float.
                if ((packedByte & 0b10000) != 0)
                {
                    return DataType.FloatingPoint32;
                }

                // Next check the top 3 (out of 5) bits and the curve type.
                var topThreeBits = (packedByte >> 2 & 0b111);
                var curveType = ChannelCurveTypeFromPackedByte(packedByte);
                return topThreeBits switch
                {
                    0b000 when curveType == CurveType.BakedTangents => DataType.FixedPoint8_7,
                    0b000 => DataType.FixedPoint9_7,
                    0b001 when curveType == CurveType.BakedTangents => DataType.FixedPoint5_10,
                    0b001 => DataType.FixedPoint5_11,
                    0b011 when curveType == CurveType.BakedTangents => DataType.FixedPoint3_12,
                    0b011 => DataType.FixedPoint3_13,
                    _ => throw new ArgumentException($"Invalid data type: {packedByte}")
                };
            }
        }
    }

    public interface IKeyFrame
    {
        public struct BakedKeyFrame : IKeyFrame
        {
            public float Data;
        }

        public struct ContinuousKeyFrame : IKeyFrame
        {
            public ushort Short1;
            public float Data;
            public short Short2;
        }

        public struct DiscontinuousKeyFrame : IKeyFrame
        {
            public ushort Short1;
            public float Data;
            public short Short2;
            public short Short3;
        }
    }

    public class AnimResourceConstBlockReader : IScenegraphDataBlockReader<AnimResourceConstBlock>
    {
        public AnimResourceConstBlock Deserialize(IoBuffer reader, PersistTypeInfo blockTypeInfo)
        {
            Debug.Assert(blockTypeInfo.Version > 5);
            var resource = ScenegraphResource.Deserialize(reader);

            // The game treats the data as a free byte array and uses this size to refer to it...
            // we just read it normally.
            var serializedSize = reader.ReadUInt32();

            var durationTicks = reader.ReadUInt16();
            var numAnimTargets = reader.ReadUInt16();
            var numEventKeys = reader.ReadUInt16();
            var dataStringLength = reader.ReadByte();
            var version = reader.ReadByte();

            var flags = reader.ReadByte();
            var priority = reader.ReadByte();
            var locomotionType = reader.ReadByte();
            var skeletonTagLength = reader.ReadByte();

            // 4 ignored uint32s.
            reader.ReadBytes(4 * 4);

            var headingOffset = reader.ReadFloat();
            var locomotionDistance = reader.ReadFloat();
            var locomotionCyclePortionCompleted = reader.ReadFloat();
            var locomotionStrideLength = reader.ReadFloat();
            var locomotionStrideVelocityMPT = reader.ReadFloat();
            var velocityMPT = reader.ReadFloat();
            var turnRotation = reader.ReadFloat();
            var preStretchFactor = reader.ReadFloat();
            var rootStretchDisplacement = reader.ReadFloat();

            // We need to note the stream position here because string data is aligned on 4-byte boundaries sometimes.
            var initialStreamPosition = reader.Stream.Position;

            var skeletonTag = reader.ReadNullTerminatedString();
            Debug.Assert(skeletonTag.Length == skeletonTagLength);
            var dataString = reader.ReadNullTerminatedString();
            Debug.Assert(dataString.Length == dataStringLength);

            Debug.Log($"skeletonTag: {skeletonTag}, dataString: {dataString}");

            ReadPadding(reader, reader.Stream.Position - initialStreamPosition);

            var animTargets = new AnimResourceConstBlock.AnimTarget[numAnimTargets];
            for (var i = 0; i < numAnimTargets; i++)
            {
                animTargets[i] = new AnimResourceConstBlock.AnimTarget();

                // 2 ignored uint32s
                reader.ReadBytes(4 * 2);

                var animType = reader.ReadUInt16();
                animTargets[i].NumSharedChannels = reader.ReadUInt16();
                animTargets[i].NumIKChains = reader.ReadByte();
                var channelType = reader.ReadByte();
                var tagLengthMaybe = reader.ReadUInt16();

                // 3 ignored uint32s.
                reader.ReadBytes(4 * 3);

                animTargets[i].Channels = new AnimResourceConstBlock.SharedChannel[animTargets[i].NumSharedChannels];
            }

            // Another aligned section... the names of each animation target's targetted tag.
            initialStreamPosition = reader.Stream.Position;
            for (var i = 0; i < numAnimTargets; i++)
            {
                animTargets[i].TagName = reader.ReadNullTerminatedString();
            }
            ReadPadding(reader, reader.Stream.Position - initialStreamPosition);

            for (var i = 0; i < numAnimTargets; i++)
            {
                var target = animTargets[i];
                for (var j = 0; j < target.NumSharedChannels; j++)
                {
                    var channel = new AnimResourceConstBlock.SharedChannel();
                    target.Channels[j] = channel;

                    // 2 ignored uint32s
                    reader.ReadBytes(4 * 2);
                    channel.BoneHash = reader.ReadUInt32();
                    reader.ReadUInt32(); // 1 ignored uint32.
                    channel.ChannelFlags = reader.ReadUInt32();
                    reader.ReadUInt32(); // 1 ignored uint32.

                    // Channel types and their order:
                    //   ChannelTypeF1
                    //   ChannelTypeF3
                    //   ChannelTypeF4
                    //   ChannelTypeQ
                    //   ChannelTypeXYZ
                    //   ChannelTypeF2
                }
            }

            // Another aligned section... the names of each of the animation channels.
            initialStreamPosition = reader.Stream.Position;
            for (var i = 0; i < numAnimTargets; i++)
            {
                var target = animTargets[i];
                for (var j = 0; j < target.NumSharedChannels; j++)
                {
                    target.Channels[j].ChannelName = reader.ReadNullTerminatedString();
                }
            }
            ReadPadding(reader, reader.Stream.Position - initialStreamPosition);

            // Next up the number of animation components in each channel and its keyframe types.
            foreach (var target in animTargets)
            {
                foreach (var channel in target.Channels)
                {
                    var numComponents = (channel.ChannelFlags >> (5 + 24)) & 0b111;
                    //var numComponents = (channel.ChannelFlags >> 5) & 0b111;
                    channel.Components = new AnimResourceConstBlock.ChannelComponent[numComponents];

                    for (var k = 0; k < numComponents; k++)
                    {
                        var component = new AnimResourceConstBlock.ChannelComponent();
                        channel.Components[k] = component;

                        component.NumKeyFrames = reader.ReadUInt16();
                        var dataType = reader.ReadByte();
                        reader.ReadBytes(1 + 4); // 1 ignored byte and 1 ignored uint32.

                        component.Type =
                            AnimResourceConstBlock.ChannelComponent.ChannelDataTypeFromSerializedPackedByte(dataType);
                        component.TangentCurveType =
                            AnimResourceConstBlock.ChannelComponent.ChannelCurveTypeFromPackedByte(dataType);
                    }
                }
            }

            // Now the actual keyframe data.
            foreach (var target in animTargets)
            {
                foreach (var channel in target.Channels)
                {
                    foreach (var component in channel.Components)
                    {
                        component.ReadKeyFrames(reader);
                    }
                }
            }

            // IK chain info for each animation target.
            foreach (var target in animTargets)
            {
                target.IKChains = new AnimResourceConstBlock.IKChain[target.NumIKChains];
                for (var i = 0; i < target.NumIKChains; i++)
                {
                    var ikChain = new AnimResourceConstBlock.IKChain();
                    target.IKChains[i] = ikChain;

                    // 2 ignored uint32s.
                    reader.ReadBytes(2 * 4);

                    var animTargetIdx = reader.ReadUInt16();
                    ikChain.NumIkTargets = reader.ReadUInt16();

                    var nameCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    // (these are technically null terminators so the crc32 can be treated as a string in game.)
                    var nameMirrorCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8

                    var intRelatedToIkStrategy = reader.ReadUInt32();

                    var beginBoneCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    var beginBoneMirrorCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    var endBoneCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    var endBoneMirrorCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    var twistVectorNameCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    var twistVectorBoneCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    var twistVectorMirrorBoneCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    var ikWeightCRC = reader.ReadUInt32();
                    reader.ReadBytes(1); // ignored uint8
                    reader.ReadUInt32(); // 1 ignored uint32
                }
            }

            // Details of each ik chain target.
            foreach (var animTarget in animTargets)
            {
                foreach (var ikChain in animTarget.IKChains)
                {
                    ikChain.IkTargets = new AnimResourceConstBlock.IKChain.Target[ikChain.NumIkTargets];
                    for (var i = 0; i < ikChain.NumIkTargets; i++)
                    {
                        var target = new AnimResourceConstBlock.IKChain.Target();
                        ikChain.IkTargets[i] = target;

                        // 2 ignored uint32s.
                        reader.ReadBytes(2 * 4);

                        var animTargetIdx = reader.ReadUInt16();
                        var ikChainIdx = reader.ReadUInt16();
                        var targetType = reader.ReadByte();

                        var boneCRC = reader.ReadUInt32();
                        reader.ReadBytes(1); // ignored uint8
                        var boneMirrorCRC = reader.ReadUInt32();
                        reader.ReadBytes(1); // ignored uint8
                        var rotationCRC = reader.ReadUInt32();
                        reader.ReadBytes(1); // ignored uint8
                        var rotation2CRC = reader.ReadUInt32();
                        reader.ReadBytes(1); // ignored uint8
                        var translationCRC = reader.ReadUInt32();
                        reader.ReadBytes(1); // ignored uint8
                        var contactCRC = reader.ReadUInt32();
                        reader.ReadBytes(1); // ignored uint8
                    }
                }
            }

            for (var i = 0; i < numEventKeys; i++)
            {
                reader.ReadUInt32(); // ignored uint32, points to the vtable internally.

                var ticks = reader.ReadUInt16();
                var type = reader.ReadInt16();
                var eventDataStringLength = reader.ReadUInt16();
                
                reader.ReadUInt32(); // ignored uint32, points to the the data string internally.
            }

            for (var i = 0; i < numEventKeys; i++)
            {
                var eventDataString = reader.ReadNullTerminatedString();
                Debug.Log($"== eventDataString:\n{eventDataString}");
            }

            foreach (var target in animTargets)
            {
                Debug.Log($"AnimTarget {target.TagName}");
                foreach (var channel in target.Channels)
                {
                    Debug.Log($"  channel name: {channel.ChannelName}, bonehash: {channel.BoneHash:X}, components: {channel.Components.Length}");

                    foreach (var component in channel.Components)
                    {
                        Debug.Log($"    numKeyFrames: {component.NumKeyFrames}, type: {component.Type}, tangentType: {component.TangentCurveType}");
                        foreach (var keyframe in component.KeyFrames)
                        {
                            Debug.Log($"      {keyframe.GetType()}");
                        }
                    }
                }
            }

            return new AnimResourceConstBlock(blockTypeInfo);
        }

        // For some reason this format pads to 4-byte boundaries sometimes...we deal with that here.
        private static void ReadPadding(IoBuffer reader, long length)
        {
            for (var i = 0; i < (length % 4); i++)
            {
                var paddingByte = reader.ReadByte();
                Debug.Assert(i == paddingByte);
            }
        }
    }
}