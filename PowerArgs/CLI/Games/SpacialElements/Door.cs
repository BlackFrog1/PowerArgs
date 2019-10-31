﻿using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class Door : Wall, IInteractable
    {
        private bool isOpen;
        public IRectangularF ClosedBounds;
        public IRectangularF OpenBounds;

        public float MaxInteractDistance => 5;
        public IRectangularF InteractionPoint => ClosedBounds;
        public bool IsOpen
        {
            get
            {
                return isOpen;
            }
            set
            {
                if (value && IsOpen)
                {
                    FindCieling().ForEach(c => c.IsVisible = false);
                }
                else if (value)
                {
                    Sound.Play("opendoor");
                    this.MoveTo(OpenBounds.Left, OpenBounds.Top);
                    FindCieling().ForEach(c => c.IsVisible = false);
                }
                else if (value == false && IsOpen == false)
                {
                    FindCieling().ForEach(c => c.IsVisible = true);
                }
                else if (value == false)
                {
                    Sound.Play("closedoor");
                    this.MoveTo(ClosedBounds.Left, ClosedBounds.Top);
                    FindCieling().ForEach(c => c.IsVisible = true);
                }
                isOpen = value;
            }
        }

        public Door()
        {
            Added.SubscribeForLifetime(() =>
            {
                this.IsOpen = this.IsOpen;
            }, this.Lifetime);
            Lifetime.OnDisposed(() => FindCieling().ForEach(c => c.Lifetime.Dispose()));
        }

        public List<Ceiling> FindCieling()
        {
            List<Ceiling> ret = new List<Ceiling>();
            if (SpaceTime.CurrentSpaceTime == null)
            {
                return ret;
            }

            foreach (var cieling in SpaceTime.CurrentSpaceTime.Elements.Where(t => t is Ceiling).Select(t => t as Ceiling).OrderBy(c => c.CalculateDistanceTo(this)))
            {
                if (cieling.CalculateDistanceTo(this) <= 1.25)
                {
                    ret.Add(cieling);
                }
                else
                {
                    foreach (var alreadyAdded in ret.ToArray())
                    {
                        if (cieling.CalculateDistanceTo(alreadyAdded) <= 1.25)
                        {
                            ret.Add(cieling);
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        public void Interact(MainCharacter character)
        {
            var newDoorDest = IsOpen ? ClosedBounds : OpenBounds;

            var charactersThatWillTouchNewDest = SpaceTime.CurrentSpaceTime.Elements.WhereAs<Character>().Where(c => c.OverlapPercentage(newDoorDest) > 0).Count();

            if (charactersThatWillTouchNewDest == 0)
            {
                IsOpen = !IsOpen;
            }
        }
    }

    [SpacialElementBinding(typeof(Door))]
    public class DoorRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context) => context.FillRect(new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Cyan), 0, 0,Width,Height);
    }

    public class DoorReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if (item.Symbol == 'd' && item.HasSimpleTag("door"))
            {
                var isDoorAboveMe = allItems.Where(i => i != item && i.Symbol == 'd' && i.Y == item.Y - 1 && item.X == item.X).Any();
                var isDoorToLeftOfMe = allItems.Where(i => i != item && i.Symbol == 'd' && i.X == item.X - 1 && item.Y == item.Y).Any();

                if (isDoorAboveMe == false && isDoorToLeftOfMe == false)
                {
                    var bigDoor = new Door();
                    hydratedElement = bigDoor;

                    var rightCount = CountAndRemoveDoorsToRight(allItems, item);
                    var belowCount = CountAndRemoveDoorsBelow(allItems, item);

                    if (rightCount > 0)
                    {
                        item.Width = rightCount + 1;
                        bigDoor.ClosedBounds = PowerArgs.Cli.Physics.RectangularF.Create(item.X, item.Y, item.Width, item.Height);
                        bigDoor.OpenBounds = PowerArgs.Cli.Physics.RectangularF.Create(bigDoor.ClosedBounds.Left + item.Width, bigDoor.ClosedBounds.Top, item.Width, item.Height);
                    }
                    else if(belowCount > 0)
                    {
                        item.Height = belowCount + 1;
                        bigDoor.ClosedBounds = PowerArgs.Cli.Physics.RectangularF.Create(item.X, item.Y, item.Width, item.Height);
                        bigDoor.OpenBounds = PowerArgs.Cli.Physics.RectangularF.Create(bigDoor.ClosedBounds.Left, bigDoor.ClosedBounds.Top+item.Height, item.Width, item.Height);
                    }
                    else
                    {
                        throw new Exception("Lonely door");
                    }


                    return true;
                }
            }

            hydratedElement = null;
            return false;
        }

        private int CountAndRemoveDoorsToRight(List<LevelItem> items, LevelItem leftMost)
        {
            var x = leftMost.X+1;

            var count = 0;
            while(true)
            {
                var adjacentItem = items.Where(i => i != leftMost && i.Symbol == 'd' && i.X == x && i.Y == leftMost.Y).SingleOrDefault();
                if(adjacentItem == null)
                {
                    break;
                }

                adjacentItem.Ignore = true;
                count++;
                x++;
            }

            return count;
        }

        private int CountAndRemoveDoorsBelow(List<LevelItem> items, LevelItem topMost)
        {
            var y = topMost.Y + 1;

            var count = 0;
            while (true)
            {
                var adjacentItem = items.Where(i => i != topMost && i.Symbol == 'd' && i.Y == y && i.X == topMost.X).SingleOrDefault();

                if(adjacentItem == null)
                {
                    break;
                }

                adjacentItem.Ignore = true;
                count++;
                y++;
            }

            return count;
        }
    }
}
