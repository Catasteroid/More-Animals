using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace MoreAnimals
{

    
    public class Core : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockBehaviorClass("NestboxCollectFrom", typeof(BehaviorCollectFromNestbox));
            api.RegisterBlockEntityClass("BlockNestbox", typeof(BlockEntityNestBox));
        }
    }


    public class BehaviorCollectFromNestbox : BlockBehavior
    {

        public BehaviorCollectFromNestbox(Block block) : base(block)
        {
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            if (!block.Code.Path.Contains("empty"))
            {
                handling = EnumHandling.PreventDefault;

                //handles the collection of items, and the transformation of the block.
                //world.Logger.Notification("Attempting to collect item(s) from target block at {0}...", blockSel.Position);
                //ItemStack[] nestboxDrops = block.GetDrops(world,blockSel.Position,byPlayer);
                //if (nestboxDrops.Length < 2)
                //{
                    //world.Logger.Notification("Nestboxdrops is from block.GetDrops is only one member long! (assuming it just to be the nest box)");
                //}
                BlockEntityNestBox getNestbox = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityNestBox;
                //world.Logger.Notification("Got the nest box entity", getNestbox);
                ItemStack[] nestboxDrops = getNestbox.GetDrops(world,blockSel.Position,byPlayer);
                ItemStack stack = null;
                //Check it's not just gonna give you a nest box as drops in place of the egg
                //world.Logger.Notification("Nestboxdrops is {0} members long.", nestboxDrops.Length);
                if (nestboxDrops != null && nestboxDrops.Length > 0)
                {
                    //world.Logger.Notification("Testing the drops..");
                    var gooddrops = 0;
                    //Test the drops to check if they're bad, had problems with null drops working on this
                    for (var i = 0; i < nestboxDrops.Length; i++)
                    {
                        //world.Logger.Notification("Drop attempt number {0}",i);
                        if (nestboxDrops[i] != null)
                        {
                            //world.Logger.Notification("Item code was valid, attempting to clone item {0}...",i);
                            stack = nestboxDrops[i].Clone();
                            //world.Logger.Notification("nestboxDrops[{0}] was valid! code: {1}, let's try to give them to the player...",i,nestboxDrops[i]);
                            if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                            {
                                //world.Logger.Notification("Couldn't put item into player's inventory, putting it onto the ground!");
                                world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                            }
                            //else
                            //{
                                //world.Logger.Notification("Successfully stuffed an item into player's inventory!");
                            //}
                            gooddrops++;
                        }
                        //else world.Logger.Notification("nestboxDrops[{0}] was null!",i);
                    }
                    if (gooddrops == 0)
                    {
                        //world.Logger.Notification("All drops were invalid for some reason, fuck this returning false...");
                        return false;
                    }
                    //Grab the last valid item
                    //BlockDropItemStack drop = new BlockDropItemStack(nestboxDrops[gooddrops]);
                    //world.Logger.Notification("Successfully collected {0} item(s) from {1}!",gooddrops, block.Code);

                    AssetLocation loc = block.Code.CopyWithPath(block.Code.Path.Replace(block.Code.Path.Split('-').Last(), "empty"));

                    world.BlockAccessor.SetBlock(world.GetBlock(loc).BlockId, blockSel.Position);

                    world.PlaySoundAt(new AssetLocation("sounds/player/collect"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                    
                }
                
                return true;
            }
            
            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (blockSel == null) return false;

            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
            return true;
        }

    }
    

    public class BlockEntityNestBox : BlockEntityHenBox, IAnimalNest
    {
        private string _fullCode;
        private List<AssetLocation> _suitableBirdNames = new List<AssetLocation>();
        private List<AssetLocation> _eggItemNames = new List<AssetLocation>();
        private Dictionary<AssetLocation, AssetLocation> _birdeggdictionary = new Dictionary<AssetLocation, AssetLocation>();
        public AssetLocation[] _chickNames = new AssetLocation[10];
        public AssetLocation[] _eggNames = new AssetLocation[10];
        private int[] _parentGenerations = new int[10];
        private double _timeToIncubate;
        private double _occupiedTimeLast;  
        public Entity _occupier;

        /*
        public override void OnBlockBroken (IPlayer byPlayer = null)
        {
            if (Api.World.Side != EnumAppSide.Server) return;
            var num = CountEggs();
            
            ItemStack[] nestboxDrops = GetDrops(Api.World,Block.Position,byPlayer);
            ItemStack stack = null;
                //Check it's not just gonna give you a nest box as drops in place of the egg
                Api.World.Logger.Notification("Nestboxdrops is {0} members long.", nestboxDrops.Length);
                if (nestboxDrops != null && nestboxDrops.Length > 0)
                {
                    Api.World.Logger.Notification("Testing the drops..");
                    var gooddrops = 0;
                    //Test the drops to check if they're bad, had problems with null drops working on this
                    for (var i = 0; i < nestboxDrops.Length; i++)
                    {
                        Api.World.Logger.Notification("Drop attempt number {0}",i);
                        if (nestboxDrops[i] != null)
                        {
                            Api.World.Logger.Notification("Item code was valid, attempting to clone item {0}...",i);
                            stack = nestboxDrops[i].Clone();
                            Api.World.Logger.Notification("nestboxDrops[{0}] was valid! code: {1}, let's try to give them to the player...",i,nestboxDrops[i]);
                            if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                            {
                                Api.World.Logger.Notification("Couldn't put item into player's inventory, putting it onto the ground!");
                                Api.World.SpawnItemEntity(stack, Block.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                            }
                            else
                            {
                                Api.World.Logger.Notification("Successfully stuffed an item into player's inventory!");
                            }
                            gooddrops++;
                        }
                        else Api.World.Logger.Notification("nestboxDrops[{0}] was null!",i);
                    }
                    if (gooddrops == 0)
                    {
                        Api.World.Logger.Notification("All drops were invalid for some reason, fuck this returning false...");
                    }
                }
        }
        */
        
        public new bool Occupied(Entity entity)
        {
            //Api.World.Logger.VerboseDebug("Checking occupied for {0}!",entity.Code.ToString());
            //if (!_occupier.Alive)
            //{}
                //Our sitting hen died at some point since we last checked
                //Api.World.Logger.VerboseDebug("Nest box _occupier fucking died, setting _occupier to null!");
            //    _occupier = null;
            //    return false;
            //}
            //Api.World.Logger.VerboseDebug("_occupier is: {0}, entity is: {1}",_occupier,entity);
            return _occupier != null && _occupier != entity;
        }

        public new void SetOccupier(Entity entity)
        {
            //if (entity == null)
            //{
            //    Api.World.Logger.VerboseDebug("Hen leaving nest!");
            //}
            //Api.World.Logger.VerboseDebug("Setting _occupier to {0}!",entity);
            _occupier = entity;
        }
        
        public ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            //Api.World.Logger.Notification("Doing BlockEntityNestBox GetDrops");
            var num = CountEggs();
            ItemStack emptyBlock = new ItemStack(Api.World.GetBlock(new AssetLocation(Block.Code.Domain + ":" +Block.FirstCodePart() + "-empty"))); 
            if (num < 1){
                //Api.World.Logger.Notification("Egg number less than 1 in BlockEntityNestBox GetDrops, returning just the nest box");
                return new ItemStack[] {};
            }
            var goodDrops = 0;
            AssetLocation[] eggDrops = new AssetLocation[num];
            for (var i = 0; i < num; i++)
            {
                if (_eggNames[i] != null)
                {
                    //Api.World.Logger.Notification("Got valid egg drop in getdrops {0}",_eggNames[i].ToString());
                    eggDrops[i] = _eggNames[i];
                    goodDrops++;
                }
                else
                {
                    eggDrops[i] = null;
                }
            }
            if (goodDrops ==0)
            {
                //Api.World.Logger.Notification("Less than 1 valid drop in BlockEntityNestBox GetDrops");
                return new ItemStack[] {};
            }
            //Api.World.Logger.Notification("At least 1 valid drop in BlockEntityNestBox GetDrops, let's add them to the array...");
            var doneDrops = 1;
            //ItemStack[] toDrop = new ItemStack[goodDrops];
            List<ItemStack> toDrop = new List<ItemStack>();
            //toDrop[0] = emptyBlock;
            for (var i = 0; i < eggDrops.Length; i++)
            {
                if (eggDrops[i] != null)
                {
                    if (Api.World.GetBlock(eggDrops[i]) != null)
                    {
                        //Api.World.Logger.Warning("Egg drop code {0} is a valid block for {1} nestbox GetDrops!",eggDrops[i],Block.Code);
                        toDrop.Add( new ItemStack(Api.World.GetBlock(eggDrops[i]),1));
                        doneDrops++;
                    }
                    else if (Api.World.GetItem(eggDrops[i]) != null)
                    {
                        //Api.World.Logger.Warning("Egg drop code {0} is a valid item for {1} nestbox GetDrops!",eggDrops[i],Block.Code);
                        toDrop.Add( new ItemStack(Api.World.GetItem(eggDrops[i]),1)); 
                        doneDrops++;
                    }
                    else
                    {
                        Api.World.Logger.Warning("Failed to resolve drop code {0} for {1} doing nestbox drops",eggDrops[i],Block.Code);
                    }
                }
            }
            return toDrop.ToArray();
        }

        public new bool IsSuitableFor(Entity entity)
        {
            //entity.World.Logger.Notification("IsSuitableFor called for {0}", entity.Code);
            //return true;
            if (!(entity is EntityAgent)) return false;
            return _suitableBirdNames.Any(name => WildcardUtil.Match(name, entity.Code));
        }
        

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            _fullCode = Block.Attributes?["fullVariant"]?.AsString() ?? "1egg";

            //Don't bother with the secondary stuff in the primary advanced stuff isn't defined
            if (Block.Attributes["suitableFor"].Exists) 
            {
                var assetCodes = Block.Attributes["suitableFor"].Token.ToObject<IEnumerable<string>>();
                _suitableBirdNames.AddRange(assetCodes.Select(p => AssetLocation.Create(p)));
                if (Block.Attributes["eggItemsFor"].Exists)
                {
                    var eggCodes = Block.Attributes["eggItemsFor"].Token.ToObject<IEnumerable<string>>();
                    _eggItemNames.AddRange(eggCodes.Select(p => AssetLocation.Create(p)));
                    for (var i = 0; i < _suitableBirdNames.Count; i++)
                    {
                        _birdeggdictionary.Add(_suitableBirdNames[i],_eggItemNames[i]);
                    }
                }
            }
            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
                RegisterGameTickListener(On1500msTick, 1500);
            }
        }

        /*
        public AssetLocation[] GetNestContents()
        {
            var num = CountEggs();
            AssetLocation[] eggdrops = new AssetLocation[num];
            if (num < 1) return eggdrops;
            for (var i = 0; i < num; i++)
            {
                if (_eggNames[i] != null)
                {
                    eggdrops[i] = _eggNames[i];
                }
                else
                {
                    eggdrops[i] = null;
                }
                
            }
            return eggdrops;
        }
        */

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("inc", _timeToIncubate);
            tree.SetDouble("occ", _occupiedTimeLast);
            for (var i = 0; i < 10; i++)
            {
                tree.SetInt("gen" + i, _parentGenerations[i]);
                var chickName = _chickNames[i];
                if (chickName != null) tree.SetString("chick" + i, chickName.ToShortString());
                var eggName = _eggNames[i];
                if (eggName != null) tree.SetString("egg" + i, eggName.ToShortString());
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            _timeToIncubate = tree.GetDouble("inc");
            _occupiedTimeLast = tree.GetDouble("occ");
            for (var i = 0; i < 10; i++)
            {
                _parentGenerations[i] = tree.GetInt("gen" + i);
                var chickCode = tree.GetString("chick" + i);
                _chickNames[i] = chickCode is null ? null : new AssetLocation(chickCode);
                var eggCode = tree.GetString("egg" + i);

                _eggNames[i] = eggCode is null ? null : new AssetLocation(eggCode);
            }
        }

        public new bool TryAddEgg(Entity entity, string chickCode, double incubationTime)
        {
            if (Block.LastCodePart() == _fullCode)
            {
                //entity.World.Logger.Notification("Entity {0} attempting to add egg to nest but it's full!",entity.Code.ToString());
                if (_timeToIncubate == 0)
                {
                    //entity.World.Logger.Notification("_timeToIncubate was zero, setting it to incubationTime!");
                    _timeToIncubate = incubationTime;
                    _occupiedTimeLast = entity.World.Calendar.TotalDays;
                }
                MarkDirty();
                return false;
            }
            _timeToIncubate = 0.0;
            var num = CountEggs();
            //entity.World.Logger.Notification("TryAddEgg called, currently {0} egg, bird: {1}, chickCode: {2}",num,entity.Code.ToString(),chickCode);
            _parentGenerations[num] = entity.WatchedAttributes.GetInt("generation");
            if (chickCode != null)
            {
                var chickAttempt = AssetLocation.Create(chickCode);
                if (entity.World.GetEntityType(chickAttempt) != null)
                {
                    //entity.World.Logger.Notification("We found a valid entity with that chick code, setting _chickNames[num] to the code!");
                    _chickNames[num] = chickAttempt;
                }
                //else entity.World.Logger.Notification("chickAttempt was invalid, _chickNames[{0}] not set!",num);
            }
            //else entity.World.Logger.Notification("chickcode was null or no chickcode was passed to TryAddEgg, _chickNames[{0}] not set and egg will be infertile!",num);
            
            if (_eggItemNames.Any())
            {
                var ecode = entity.Code;
                //entity.World.Logger.Notification("We have valid egg names, attempting to match entity code for {0}",ecode);
                //_suitableBirdNames.Any(name => WildcardUtil.Match(name, entity.Code));
                   
                if (_suitableBirdNames.Any(name => WildcardUtil.Match(name, ecode)))
                {
                    //entity.World.Logger.Notification("Matched the entity code! Now get the egg item code for that entity");
                    //entity.World.Logger.Notification("Egg {0} for {1}",_birdeggdictionary[ecode],ecode);
                    _eggNames[num] =_birdeggdictionary[ecode];

                    //_eggNames[i] = _eggItemNames[i];
                }
                //else
                //{
                //    entity.World.Logger.Notification("Did not match an egg item code to the entity code!");
                //}
            }
            num++;
            var block = Api.World.GetBlock(new AssetLocation(Block.Code.Domain + ":" +Block.FirstCodePart() + "-" + num + (num > 1 ? "eggs" : "egg")));
            if (block == null)
            {
                return false;
            }
            //entity.World.Logger.Notification("Current nest contents is {0} eggs, current chick and egg item codes of contents:",num);
            //for (var i = 0; i < num; i++)
            //{
            //    entity.World.Logger.Notification("Egg {0} - chickCode: {1}, eggItem: {2}",num,_chickNames[i],_eggNames[i]);
            //}
            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            Block = block;
            MarkDirty();
            return true;
        }

        private int CountEggs()
        {
            var eggs = Block.LastCodePart()[0];
            var eggReturn = eggs <= '9' && eggs >= '0' ? eggs - '0' : 0;
            //Api.World.Logger.Notification("Doing CountEggs, Block lastcodepart: {0}, eggs: {1}, eggReturn: {2}",Block.LastCodePart(),eggs,eggReturn);
            return eggReturn;
        }
        
        private void On1500msTick(float dt)
        {
            if (_timeToIncubate == 0) return;

            double newTime = Api.World.Calendar.TotalDays;
            if (_occupier != null && _occupier.Alive)   //Does this need a more sophisticated check, i.e. is the _occupier's position still here?  (Also do we reset the _occupier variable to null if save and re-load?)
            {
                //Api.World.Logger.Notification("Checking ig newtime is larger than _occupiedTimeLast, Nest box is occupied by {0}, newTime: {1}, _occupiedTimeLast: {2}, _timeToIncubate: {3}",_occupier.Code.ToString(),newTime,_occupiedTimeLast,_timeToIncubate);
                if (newTime > _occupiedTimeLast)
                {
                    //Api.World.Logger.Notification("Nest box is occupied by {0}, newTime: {1}, _occupiedTimeLast: {2}, _timeToIncubate: {3}",_occupier.Code.ToString(),newTime,_occupiedTimeLast,_timeToIncubate);
                    _timeToIncubate -= newTime - _occupiedTimeLast;
                    this.MarkDirty();
                }
            }
            _occupiedTimeLast = newTime;
            //Api.World.Logger.Notification("Setting newTime to _occupiedTimeLast, newTime: {0}, _occupiedTimeLast: {1}, _timeToIncubate: {2}",newTime,_occupiedTimeLast,_timeToIncubate);
            if (_timeToIncubate <= 0)
            {
                
                _timeToIncubate = 0;
                int eggs = CountEggs();
                var entitiesSpawned = 0;
                var entitiesFailed = 0;
                Random rand = Api.World.Rand;
                //Api.World.Logger.Notification("_timeToIncubate is smaller than zero, time for eggs to hatch! egg number: {0}, rand: {1}",eggs,rand.ToString());
                for (int c = 0; c < eggs; c++)
                {
                    //Api.World.Logger.Notification("Egg attempt number {0}/{1}",c,eggs);
                    AssetLocation chickName = _chickNames[c];
                    if (chickName == null)
                    {
                        //Api.World.Logger.Notification("_chickNames[{0}] was null! {1}",c,chickName.ToString());
                        entitiesFailed++;
                        continue;    
                    }
                    int generation = _parentGenerations[c];

                    EntityProperties childType = Api.World.GetEntityType(chickName);
                    if (childType == null)
                    {
                        //Api.World.Logger.Notification("childType {0} was null, either chickName[{1}] ({2}) was weird, broken not a valid entity",childType,c,chickName);
                        entitiesFailed++;
                        continue;
                    }
                    Entity childEntity = Api.World.ClassRegistry.CreateEntity(childType);
                    if (childEntity == null)
                    {
                        //Api.World.Logger.Notification("childEntity {0} was null, either the childType {1} (chickName {2}) was weird, broken not a valid entity",childEntity,childType,chickName);
                        entitiesFailed++;
                        continue;
                    }
                    childEntity.ServerPos.SetFrom(new EntityPos(this.Position.X + (rand.NextDouble() - 0.5f) / 5f, this.Position.Y, this.Position.Z + (rand.NextDouble() - 0.5f) / 5f, (float) rand.NextDouble() * GameMath.TWOPI));
                    childEntity.ServerPos.Motion.X += (rand.NextDouble() - 0.5f) / 200f;
                    childEntity.ServerPos.Motion.Z += (rand.NextDouble() - 0.5f) / 200f;

                    //BUGGERED SILLY find out why later
                    //childEntity.Pos.SetFrom(childEntity.ServerPos);
                    
                    //Api.World.Logger.Notification("Successfully spawned entity ({0}) in attempt number {1}, parent/child generation: {2}/{3}",childEntity,c,generation,generation+1);
                    childEntity.Attributes.SetString("origin", "reproduction");
                    childEntity.WatchedAttributes.SetInt("generation", generation + 1);
                    Api.World.SpawnEntity(childEntity);
                    entitiesSpawned++;
                }

                //Api.World.Logger.Notification("Finished spawning entities, {0} were spawned of {1} eggs, {2} spawnings failed",eggs,entitiesSpawned,entitiesFailed);
                //Exchanges the nest box with the empty variant, this actually deletes any infertile eggs remaining in the box
                Block replacementBlock = Api.World.GetBlock(new AssetLocation(Block.Code.Domain + ":" + Block.FirstCodePart() + "-empty"));
                Api.World.BlockAccessor.ExchangeBlock(replacementBlock.Id, this.Pos);
                this.Api.World.SpawnCubeParticles(Pos.ToVec3d().Add(0.5, 0.5, 0.5), new ItemStack(this.Block), 1, 20, 1, null);
                this.Block = replacementBlock;
            }
        }
        
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            int eggCount = CountEggs();
            int fertileCount = 0;
            //string eggitemstring = "";
            if (eggCount > 0)
            {
                dsc.AppendLine("Nest box contents: ");
                //Api.World.Logger.VerboseDebug("Nest box egg count larger than zero, number of eggs: {0}",eggCount);
                for (int i = 0; i < eggCount; i++)
                {
                    //Api.World.Logger.VerboseDebug("ChickCode for i is: {0}",_chickNames[i]);
                        //Api.World.Logger.Notification("We have a valid chickCode for this fertile egg! It is: {0}",_chickNames[i].ToString());
                        if (_eggNames[i] != null)
                        {
                            //Api.World.Logger.VerboseDebug("And we have a valid egg item name for this fertile egg too! It is: {0}",_eggNames[i].ToString());
                            if (_chickNames[i] != null) 
                            {
                                if (Api.World.GetBlock(_eggNames[i]) != null) dsc.AppendLine("• " + Lang.Get(_eggNames[i].Domain + ":block-" + _eggNames[i].Path.ToString()) + " (fertile)");
                                else dsc.AppendLine("• " + Lang.Get(_eggNames[i].Domain + ":item-" + _eggNames[i].Path.ToString()) + " (fertile)");
                                fertileCount++;
                            }
                            else
                            {
                                if (Api.World.GetBlock(_eggNames[i]) != null) dsc.AppendLine("• " + Lang.Get(_eggNames[i].Domain + ":block-" + _eggNames[i].Path.ToString()));
                                else dsc.AppendLine("• " + Lang.Get(_eggNames[i].Domain + ":item-" + _eggNames[i].Path.ToString()));
                                
                            }
                            //eggitemstring = eggitemstring + Lang.Get("item-" + _eggNames[i]);
                            //if (i > 0) eggitemstring = eggitemstring + ", ";
                            
                        }
                        else
                        {
                            //Api.World.Logger.VerboseDebug("We didn't have a valid eggitem name for egg {0} in the box, using this instead: {1}",i,"egg (" + _chickNames[i] + ")");
                            if (_chickNames[i] != null)
                            {
                                dsc.AppendLine("• egg (" + Lang.Get(_chickNames[i].ToString())+ ", fertile)");
                                fertileCount++;
                            }
                            else
                            {
                                dsc.AppendLine("• egg (" + Lang.Get(_chickNames[i].ToString())+ ")");
                            }
                            
                        }
                    
                    //Api.World.Logger.Notification("Eggitemstring so far: {0}",eggitemstring);
                }
            }
            else
            {
                dsc.AppendLine("Nest box contents: Nothing");
            }

            //if (eggitemstring.Length > 0)
            //{ 
            //    Api.World.Logger.Notification("Nest box egg count larger than zero, number of eggs: {0}",eggCount);
            //    dsc.AppendLine("Nest box contents: " + eggitemstring);
            //}
            //elseW
            //{
            //    dsc.AppendLine("Nest box contents: Nothing");
            //}
    
            if (fertileCount > 0)
            {
                //if (fertileCount > 1)
                //    dsc.AppendLine(Lang.Get("{0} fertile eggs", fertileCount));
                //else
                //    dsc.AppendLine(Lang.Get("1 fertile egg"));


                if (_timeToIncubate >= 1.5)
                    dsc.AppendLine(Lang.Get("Incubation time remaining: {0:0} days", _timeToIncubate));
                else if (_timeToIncubate >= 0.75)
                    dsc.AppendLine(Lang.Get("Incubation time remaining: 1 day"));
                else if (_timeToIncubate > 0)
                    dsc.AppendLine(Lang.Get("Incubation time remaining: {0:0} hours", _timeToIncubate * 24));

                if (_occupier == null && Block.LastCodePart() == _fullCode)
                    dsc.AppendLine(Lang.Get("A broody hen is needed!"));
                else if (_occupier != null)
                {
                    dsc.AppendLine(Lang.Get("A broody hen is currently sitting on the eggs"));
                }
            }
            else if (eggCount > 0)
            {
                dsc.AppendLine(Lang.Get("No eggs are fertilized"));
            }

            string d = "";
            d = "Nest box is suitable for: ";
            if (_suitableBirdNames.Count == 1)
            {
                dsc.AppendLine(d + Lang.Get(_suitableBirdNames[0].ToString()));
                //d = d + Lang.Get(_suitableBirdNames[0].ToString());
            }
            else if (_suitableBirdNames.Count == 0)
            {
                dsc.AppendLine(d + "No birds");
            }
            else 
            {
                dsc.AppendLine(d);
                for (int i = 0; i < _suitableBirdNames.Count; i++)
                {
                    
                    dsc.AppendLine("• " + Lang.Get(_suitableBirdNames[i].Domain + ":item-creature-" + _suitableBirdNames[i].Path.ToString()));
                    //string toLang = _suitableBirdNames[i].Domain + ":item-creature-" + _suitableBirdNames[i].Path.ToString();
                    //d = d + Lang.Get(toLang);
                    //if (i < _suitableBirdNames.Count-1) d = d + ", ";
                    //dsc.AppendLine($"• {d}");
                }
            }
        }
    }
}