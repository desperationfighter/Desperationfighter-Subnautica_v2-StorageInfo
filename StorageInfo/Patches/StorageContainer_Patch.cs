using HarmonyLib;

//transpiller
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace StorageInfo.Patches
{
    [HarmonyPatch(typeof(StorageContainer))]
    [HarmonyPatch(nameof(StorageContainer.OnHandHover))]
    public static class StorageContainer_OnHandHover_Patch
    {
        //Debug Logging - Deactivate before shipping
        private static bool debuglogging = false;
        //Deep Logging
        private static bool deeplogging = false;

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            string classnamefordocu = "StorageContainer_OnHandHover_Patch";
            if (debuglogging && !deeplogging)
            {
                Plugin.Logger.LogDebug( "Deeploging deactivated");
            }

            if (debuglogging)
            {
                Plugin.Logger.LogDebug($"Start Transpiler - {classnamefordocu}");
            }

            var getFullState = typeof(StorageContainer_OnHandHover_Patch).GetMethod("Getfullstate", BindingFlags.Public | BindingFlags.Static);
            var stringEmpty = AccessTools.Field(typeof(string), "Empty");
            bool found = false;
            var Index = -1;
            var codes = new List<CodeInstruction>(instructions);

            //logging before
            if (debuglogging && deeplogging)
            {
                Plugin.Logger.LogDebug( "Deep Logging pre-transpiler:");
                for (int k = 0; k < codes.Count; k++)
                {
                    Plugin.Logger.LogDebug( (string.Format("0x{0:X4}", k) + $" : {codes[k].opcode.ToString()}	{(codes[k].operand != null ? codes[k].operand.ToString() : "")}"));
                }
            }

            //analyse the code to find the right place for injection
            if (debuglogging)
            {
                Plugin.Logger.LogDebug( "Start code analyses");
            }
            for (var i = 0; i < codes.Count; i++)
            {
                /*
                1 IL_003F: call instance bool StorageContainer::IsEmpty()
                2 IL_0044: brtrue.s IL_004D
                3 IL_0046: ldsfld    string[mscorlib] System.String::Empty
                4 IL_004B: br.s IL_0052
                5 IL_004D: ldstr     "Empty"
                 */

                if (codes[i].opcode == OpCodes.Call && codes[i + 2].opcode == OpCodes.Ldsfld && codes[i + 4].opcode == OpCodes.Ldstr)
                {
                    if (debuglogging)
                    {
                        Plugin.Logger.LogDebug( "Found IL Code Line for Index");
                        Plugin.Logger.LogDebug( $"Index = {Index.ToString()}");
                    }
                    found = true;
                    Index = i;
                    break;
                }
            }

            if (debuglogging)
            {
                if (found)
                {
                    Plugin.Logger.LogDebug( "found true");
                }
                else
                {
                    Plugin.Logger.LogDebug( "found false");
                }
            }

            if (Index > -1)
            {
                if (debuglogging)
                {
                    Plugin.Logger.LogDebug( "Index1 > -1");
                }
                Plugin.Logger.LogInfo($"Transpiler injectection position found - {classnamefordocu}");
                codes[Index] = new CodeInstruction(OpCodes.Call, getFullState);
                codes.RemoveRange(Index + 1, 4);
            }
            else
            {
                Plugin.Logger.LogError("Index was not found");
            }

            //logging after
            if (debuglogging && deeplogging)
            {
                Plugin.Logger.LogDebug( "Deep Logging after-transpiler:");
                for (int k = 0; k < codes.Count; k++)
                {
                    Plugin.Logger.LogDebug( (string.Format("0x{0:X4}", k) + $" : {codes[k].opcode.ToString()}	{(codes[k].operand != null ? codes[k].operand.ToString() : "")}"));
                }
            }

            if (debuglogging)
            {
                Plugin.Logger.LogDebug( "Transpiler end going to return");
            }
            return codes.AsEnumerable();
        }

        public static string Getfullstate(StorageContainer _storageContainer)
        {
            if (debuglogging)
            {
                Plugin.Logger.LogDebug( "call Getfullstate");
            }

            var items = _storageContainer.container.GetItemTypes();
            int itemscount = _storageContainer.container.count;
            if (debuglogging)
            {
                Plugin.Logger.LogDebug( $"Itemcount all Items = {itemscount.ToString()}");
            }
            int origSize = _storageContainer.container.sizeX * _storageContainer.container.sizeY;
            if (debuglogging)
            {
                Plugin.Logger.LogDebug( $"Original Max Size = {origSize.ToString()}");
            }
            int usedSize = 0;
            foreach (var i in items)
            {
                var size = CraftData.GetItemSize(i);
                int numberofsingletechtype = (_storageContainer.container.GetItems(i)).Count;
                if (debuglogging)
                {
                    Plugin.Logger.LogDebug( $"Techtype = {i.ToString()}");
                    Plugin.Logger.LogDebug( $"Number of items in this Techtype = {numberofsingletechtype.ToString()}");
                }
                usedSize += size.x * size.y * numberofsingletechtype;
                if (debuglogging)
                {
                    Plugin.Logger.LogDebug( $"Used Space of this Techtype = {(size.x * size.y * numberofsingletechtype).ToString()}");
                }
            }
            var sizeLeft = origSize - usedSize;
            if (debuglogging)
            {
                Plugin.Logger.LogDebug( $"Used Space off all Techtypes = {usedSize.ToString()}");
            }

            StringBuilder stringBuilder = new StringBuilder();
            if (!_storageContainer.container.HasRoomFor(1, 1))
            {
                if (debuglogging)
                {
                    Plugin.Logger.LogDebug( "Container is Full - way");
                }
                stringBuilder.AppendLine("Full - " + itemscount + " Items stored");
                stringBuilder.AppendLine($"{sizeLeft} of {origSize} free");
            }
            else if (_storageContainer.IsEmpty())
            {
                if (debuglogging)
                {
                    Plugin.Logger.LogDebug( "Container is empty - way");
                }
                stringBuilder.AppendLine("Empty");
                stringBuilder.AppendLine($"{sizeLeft} of {origSize} free");
            }
            else
            {
                if (debuglogging)
                {
                    Plugin.Logger.LogDebug( "Container Contains X Item - way");
                }
                stringBuilder.AppendLine(itemscount + " Items - " + usedSize + " used");
                stringBuilder.AppendLine($"{sizeLeft} of {origSize} free");
            }
            return stringBuilder.ToString();
        }
    }
}
