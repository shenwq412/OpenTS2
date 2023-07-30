﻿using OpenTS2.Common;
using OpenTS2.Content;
using OpenTS2.Content.DBPF.Scenegraph;
using OpenTS2.Files.Formats.DBPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenTS2.Rendering.Materials
{
    public class StandardMaterial : AbstractMaterial
    {
        public override string Name => "StandardMaterial";

        private static readonly int AlphaCutOff = Shader.PropertyToID("_AlphaCutOff");
        private static readonly int AlphaMultiplier = Shader.PropertyToID("_AlphaMultiplier");
        private static readonly int SeaLevel = Shader.PropertyToID("_SeaLevel");

        public override Material Parse(ScenegraphMaterialDefinitionAsset definition)
        {
            Shader shader;
            // Decide which shader to use based on the alpha blending and alpha testing.
            if (definition.GetProperty("stdMatAlphaTestEnabled", defaultValue: "0") == "1")
            {
                shader = Shader.Find("OpenTS2/StandardMaterial/AlphaCutOut");
            }
            else if (definition.GetProperty("stdMatAlphaBlendMode", defaultValue: "none") == "blend")
            {
                shader = Shader.Find("OpenTS2/StandardMaterial/AlphaBlended");
            }
            else
            {
                shader = Shader.Find("OpenTS2/StandardMaterial/Opaque");
            }

            var material = new Material(shader);

            // Adjust the material properties based on the corresponding keys.
            foreach (var property in definition.MaterialDefinition.MaterialProperties)
            {
                switch (property.Key)
                {
                    case "stdMatAlphaRefValue":
                        var alphaCutoffValue = int.Parse(property.Value);
                        material.SetFloat(AlphaCutOff, alphaCutoffValue / 255.0f);
                        break;
                    case "stdMatBaseTextureName":
                        var textureName = property.Value;
                        var texture = ContentProvider.Get().GetAsset<ScenegraphTextureAsset>(
                            new ResourceKey(textureName + "_txtr", definition.GlobalTGI.GroupID, TypeIDs.SCENEGRAPH_TXTR)
                        );
                        definition.Textures.Add(texture);
                        material.mainTexture = texture.GetSelectedImageAsUnityTexture(ContentProvider.Get());
                        break;
                    case "stdMatAlphaMultiplier":
                        material.SetFloat(AlphaMultiplier, float.Parse(property.Value));
                        break;
                }
            }

            var neighborhood = NeighborhoodManager.CurrentNeighborhood;
            if (neighborhood != null)
            {
                material.SetFloat(SeaLevel, NeighborhoodManager.CurrentNeighborhood.Terrain.SeaLevel);
            }

            return material;
        }
    }
}