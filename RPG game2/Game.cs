using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace RPG_game2
{
    public class Game
    {
        PictureBox GraphicalIF; //Graphical Interface
        public Form gameForm;
        PlayerParty playerParty;
        WorldMap worldMap;
        WMMonster wmmonster;
        bool inEncounter;
        Battle battle;

        //Constructor
        public Game(Form form)
        {
            //set gameform
            gameForm = form;
            
            //Setup Graphical Interface
            GraphicalIF = new PictureBox();
            GraphicalIF.Height = gameForm.Height;
            GraphicalIF.Width = gameForm.Width;
            GraphicalIF.BackColor = Color.Cyan;
            GraphicalIF.Parent = gameForm;

            //Create World
            worldMap = new WorldMap(gameForm);
            playerParty = new PlayerParty(new Point(2, 2), 2);
            wmmonster = new WMMonster(new Point(3, 3), 1);

            //Ready to Draw
            DrawAll();

        }

        //DrawAll() determines what state the game is in and draws background and sprites appropriately
        void DrawAll()
        {
            //set up a new image untowhich graphics will be drawn by the Graphics device
            Image image = new Bitmap(gameForm.Width, gameForm.Height);
            Graphics device;
            device = Graphics.FromImage(image);

            //determine state, Possibly replace this with an enumerated switch case structure
            if (!inEncounter)
            {
                worldMap.DrawMap(device);
                playerParty.partySprite.DrawSprite(device);
                wmmonster.monsterSprite.DrawSprite(device);
            }
            else
            {
                battle.DrawBattle(device);

            }

            //Graphical interface is updated and refreshed
            GraphicalIF.Image = image;
            GraphicalIF.Refresh();
        }

        //HandleKeyPress determines what state the game is in and determine how to respond to key presses, then it draws any updates (redraws everything)
        public void HandleKeyPress(KeyEventArgs e)
        {
            if (!inEncounter)
            {
                //depending on arrow key pressed the playerparty will move in the desired location
                if (e.KeyCode == Keys.Up) { playerParty.Move(0, -1); }
                if (e.KeyCode == Keys.Down) { playerParty.Move(0, 1); }
                if (e.KeyCode == Keys.Right) { playerParty.Move(1, 0); }
                if (e.KeyCode == Keys.Left) { playerParty.Move(-1, 0); }

                //if statement determines if a monster has been discovered and will start a battle if so
                if (CollisionDetector(playerParty.location) == 1)
                {
                    inEncounter = true;
                    playerParty.Move(1, 1);
                    battle = new Battle(1, playerParty);
                }
            }
            else
            {
                //if in battle the battle's keypress method will be called
                battle.KeyPress(e);

                //if the current key press sets the battle to combat phase then a combat will be conducted followed by a victory check
                if (battle.inCombat)
                {
                    //combat phase must be called here because the combat will require the Graphical IF in order to draw the combat sequence
                    //I should move this into the battle class and send a reference of the graphicalIF to the battle when initiailly constructing it
                    //This should allow the battle to manipulate the GraphicalIF by itself
                    battle.StartCombat(GraphicalIF);
                    battle.inCombat = false;

                    //victory check and then either ending the match or starting the next round
                    battle.VictoryCheck();
                    if (battle.victory || battle.defeat)
                    {
                        inEncounter = false;

                    }
                    else
                    {
                        battle.StartNewRound();
                    }
                }
            }

            //Key press handled, redraw the GraphicalIF to reflect changes
            DrawAll();

        }

        // CollisionDetector method returns different ints depending on what is located at a particular location on the world map
        public int CollisionDetector(Point location)
        {
            int result;
            if (location == wmmonster.location)
            {
                result = 1;
            }
            else
            {
                result = 0;
            }
            return result;

        }
    }

    //World Map//////////////////////////////////////////////////////

    //World Map Monster has an image, location, ID determines type of battle
    class WMMonster
    {
        public Point location;
        public int ID;
        public WorldMapSprite monsterSprite;

        public WMMonster(Point location, int ID)
        {
            //save given location and ID
            this.location = location;
            this.ID = ID;

            //setup the world map sprite
            Image image;
            image = new Bitmap("monster.png");
            monsterSprite = new WorldMapSprite(image, location);
        }

    }

    //World map contains method for drawing the world map, a 10 * 10 grid of tiles
    class WorldMap
    {
        Image worldMapSprite; //not really used I don't think...

        public WorldMap(Form form)
        {
            worldMapSprite = new Bitmap(form.Width, form.Height);
        }

        //DrawMap goes through each tile in grid and uses DrawTile
        public void DrawMap(Graphics device)
        {

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    DrawTile(device, 1, new Point(x * 40, y * 40));
                }
            }
        }

        //Drawtile
        void DrawTile(Graphics device, int ID, Point location)
        {
            Image img;


            switch (ID)
            {
                default:
                    img = new Bitmap("Grass.png");
                    break;
            }
            device.DrawImage(img, location);
        }
    }

    //Contains method for drawing the sprites of characters and objects onto the world map
    class WorldMapSprite
    {
        Image image;
        Point location;

        //when a world Map Sprite is constructed it requires an image a map location, this location is converted into pixel co-ordinates
        public WorldMapSprite(Image image, Point location)
        {
            this.image = image;
            this.location.X = location.X * 40;
            this.location.Y = location.Y * 40;
        }

        //Draw current sprite onto map
        public void DrawSprite(Graphics device)
        {
            device.DrawImage(image, location);
        }

        //moves sprite by one map tile
        public void Move(int x, int y)
        {
            location.X += 40 * x;
            location.Y += 40 * y;
        }
    }

    //////////////////////////////////////////////Battle////////////////////////////////////////////////////////
    //Battles consist of rounds, at the start of a round the user enters the attacks/techniques and then the target for each hero
    //the attack and target are stored on the hero class, 

    class Battle
    {
        //declaring properties
        public bool victory, defeat;
        public bool inTargeting, inTech, inCombat, isFirstHero;
        public int currentHero;
        public BattleMap battleMap;
        public TechniqueMenu techMenu;
        public TargetMenu targetMenu;
        public List<Unit> monsterList;
        public List<Technique> validTechniques;
        public List<Unit> validTargets;
        public List<string> validTargetNames;
        public List<Hero> heroList;
        public Combat combat;
        public FlavourText flavourText;

        //BattleConstructor
        public Battle(int ID, PlayerParty playerParty)
        {
            victory = false;
            defeat = false;
            monsterList = new List<Unit>();
            heroList = new List<Hero>();
            flavourText = new FlavourText();
            techMenu = new TechniqueMenu();
            targetMenu = new TargetMenu();
            switch (ID)
            {
                default:
                    Monster monster1 = new Monster(0, MonsterID.Demon);
                    Monster monster2 = new Monster(1, MonsterID.Demon2);
                    Monster monster3 = new Monster(2, MonsterID.Demon);
                    Monster monster4 = new Monster(3, MonsterID.Demon2);
                    monsterList.Add(monster1);
                    monsterList.Add(monster2);
                    monsterList.Add(monster3);
                    monsterList.Add(monster4);
                    heroList = playerParty.heros;
                    break;
            }
            for (int x = 0; x < heroList.Count; x++)
            {
                heroList[x].HP = heroList[x].Health;
            }
            for (int x = 0; x < monsterList.Count; x++)
            {
                monsterList[x].HP = monsterList[x].Health;
            }

            StartNewRound();
            inTech = true;
            inTargeting = false;
            inCombat = false;
            battleMap = new BattleMap();


        }

        //commences new round and updates the tech and target menus
        public void StartNewRound()
        {
            //reset hero and find valid techniques for the current hero and valid targets for the highlighted technique
            currentHero = 0;

            if(heroList[currentHero].isDead)
            {
                for (int x = 1; x < heroList.Count; x++)
                {
                    if(!heroList[x].isDead)
                    {
                        currentHero = x;
                        break;
                    }
                }
            }

            //activates the technique menu
            ActivateTechniqueMenu();

        }

        //handles changing to the "choose Technique" stageneeds to be able to work from StartNewRound, a Cancel and for sequential hero
        public void ActivateTechniqueMenu()
        {
            //finds valid techniques for current character
            ValidateTechniques();

            //displayes these valid techniques on the tech menu
            techMenu.Update(validTechniques);



            //set battle stage
            combat = null;
            inCombat = false;
            inTargeting = false;
            inTech = true;

            //highlight current menu
            targetMenu.Dim();
            techMenu.Activate();

            //finds and displays possible targets
            FindValidTargets();
            targetMenu.Update(validTargets);
        }

        public void ActivateTargetMenu()
        {
            FindValidTargets();

            //for (int x = 0; x < monsterList.Count; x++)
            //{
            //    if (!monsterList[x].isDead)
            //    {
            //        validTargets.Add(monsterList[x]);
            //        validTargetNames.Add(monsterList[x].name);
            //    }
            //}
            inTech = false;
            inTargeting = true;
            techMenu.Dim();
            targetMenu.Activate();
        }

        public void StartCombat(PictureBox GraphicalIF)
        {
            inTargeting = false;
            inCombat = true;
            targetMenu.Dim();
            Graphics device;
            Image background = new Bitmap(GraphicalIF.Width, GraphicalIF.Height);
            device =Graphics.FromImage(background);
            device.DrawImage(battleMap.image, new Point(0, 0));
            device.DrawImage(techMenu.menu.box, techMenu.menu.boxLoc);
            device.DrawImage(targetMenu.menu.box, targetMenu.menu.boxLoc);

            combat = new Combat(heroList, monsterList, flavourText, GraphicalIF, background);
        }

        public void DrawBattle(Graphics device)
        {
            battleMap.DrawBattleMap(device);
            for (int x = 0; x < heroList.Count; x++)
            {
                heroList[x].DrawUnit(device);
            }
            for (int x = 0; x < monsterList.Count; x++)
            {
                monsterList[x].DrawUnit(device);
            }


            techMenu.DrawAttackMenu(device);

            targetMenu.DrawTargetMenu(device);

            flavourText.DrawFlavourText(device);
        }

        public void Cancel()
        {
            if(inTech)
            {
                currentHero--;
                ActivateTargetMenu();
            }
            else if(inTargeting)
            {
                ActivateTechniqueMenu();
            }
        }

        public void KeyPress(KeyEventArgs e)
        {
            if (inTech)
            {
                if (e.KeyCode == Keys.Up)
                {
                    techMenu.menu.cursor.Move(-1);
                    FindValidTargets();
                    targetMenu.Update(validTargets);
                }
                if (e.KeyCode == Keys.Down)
                {
                    techMenu.menu.cursor.Move(1);
                    FindValidTargets();
                    targetMenu.Update(validTargets);

                }
                if (e.KeyCode == Keys.Return)
                {
                    heroList[currentHero].cA = techMenu.Select();
                    if (heroList[currentHero].cA != heroList[currentHero].cancel)
                    {
                        flavourText.AddLine(String.Format("{0} selected {1}", heroList[currentHero].name, heroList[currentHero].cA.name));
                        ActivateTargetMenu();

                        //set hero to remember valid targets?
                    }
                    else
                    {
                        Cancel();
                    }
                }
            }
            else if (inTargeting)
            {
                if (e.KeyCode == Keys.Up) { targetMenu.menu.cursor.Move(-1); }
                if (e.KeyCode == Keys.Down) { targetMenu.menu.cursor.Move(1); }
                if (e.KeyCode == Keys.Return)
                {

                    heroList[currentHero].cA.Target(validTargets[targetMenu.menu.cursor.POS]);
                    //flavourText.AddLine(String.Format("{0} targets {1}", heroList[currentHero].name,  monsterList[heroList[currentHero].target].name));

                    inTargeting = false;

                    if (currentHero == (heroList.Count - 1))
                    {
                        inCombat = true;
                    }
                    else
                    {
                        inTech = true;
                        currentHero++;
                        ActivateTechniqueMenu();
                    }

                }
            }
        }

        public void VictoryCheck()
        {
            victory = true;
            defeat = true;
            for (int x = 0; x < monsterList.Count; x++)
            {
                if (!monsterList[x].isDead)
                {
                    victory = false;
                }
            }
            for (int x = 0; x < heroList.Count; x++)
            {
                if (!heroList[x].isDead)
                {
                    defeat = false;
                }
            }
        }

        //will update the validTargets list for the currently selected technique
        public void FindValidTargets()
        {
            validTargetNames = new List<string>();
            //will only find targets for techniques other than cencel
            if (techMenu.Select() != heroList[currentHero].cancel)
            {
                validTargets = new List<Unit>();

                //will go through the valid targets for the currently selected technique
                for (int z = 0; z < heroList[currentHero].techList[techMenu.menu.cursor.POS].validTargets.Count; z++)
                {
                    //goes through each monster and adds them to the possible target list if present and not dead
                    for (int x = 0; x < monsterList.Count; x++)
                    {
                        if (!monsterList[x].isDead && monsterList[x].POS == heroList[currentHero].techList[techMenu.menu.cursor.POS].validTargets[z])
                        {
                            validTargetNames.Add(monsterList[x].name);
                            validTargets.Add(monsterList[x]);
                        }
                    }

                }
            }

            validTargetNames.Add("Cancel");
            validTargets.Add(new Monster(5, MonsterID.cancel));
            targetMenu.Update(validTargets);
        }

        public void ValidateTechniques()
        {
            validTechniques = new List<Technique>();
            bool hasValidated = false;

            //select each technique with firstr for loop
            for (int x = 0; x < heroList[currentHero].techList.Count; x++)
            {
                hasValidated = false;

                //select each valid target of the technique, the valid target represents a position this attack can hit
                for (int y = 0; y < heroList[currentHero].techList[x].validTargets.Count; y++)
                {

                    //for each monster the techique could be validated if the monsters position matches with a valid target and isn't already dead
                    for (int z = 0; z < monsterList.Count; z++)
                    {
                        //if the current technique's possible target is present and alive the technique will be validated by using hasValidated and adding it to the valid technique list
                        if (heroList[currentHero].techList[x].validTargets[y] == monsterList[z].POS && !monsterList[z].isDead && !hasValidated)
                        {
                            validTechniques.Add(heroList[currentHero].techList[x]);
                            hasValidated = true;
                        }

                    }
                }
            }

            //adds cancel button to valid techniques if necessary
            if(currentHero!=0)
            {
                validTechniques.Add(heroList[currentHero].cancel);
            }

            //lets the hero rememebr what techniques are valid this round
            heroList[currentHero].validTechs = validTechniques;

        }

        /* public void DrawAttack(Unit Instigator, Unit Target, PictureBox image)
         {
             Graphics device;
             device = Graphics.FromImage(image.Image);

             Point originalIlocation, originalTlocation;
             Image  originalIimage, originalTimage;
             originalIlocation = Instigator.battleSprite.location;
             originalIimage = Instigator.battleSprite.image;
             originalTlocation = Target.battleSprite.location;
             originalTimage = Target.battleSprite.image;



             Instigator.battleSprite.location = new Point(70, 50);
             Target.battleSprite.location = new Point(80, 50);

             Instigator.battleSprite.DrawBattleSprite(device);
             Target.battleSprite.DrawBattleSprite(device);

             System.Threading.Thread.Sleep(500);

             Instigator.battleSprite.ResetPosition();
             Target.battleSprite.ResetPosition();


             Instigator.battleSprite.DrawBattleSprite(device);
             Target.battleSprite.DrawBattleSprite(device);
         }*/
    }

    class BattleMap
    {
        public Image image;

        public BattleMap()
        {
            image = new Bitmap("grasslandBattle.png");

        }

        public void DrawBattleMap(Graphics device)
        {
            device.DrawImage(image, new Point(0, 0));

        }
    }

    class TechniqueMenu
    {
        public Menu menu;
        Point location;
        private List<Technique> techList;

        public TechniqueMenu()
        {
            this.location = new Point(-5, 255);
            menu = new Menu(new List<string>(), this.location);
            Image attackBox = new Bitmap("TechniqueMenuBox.png");
        }

        public void DrawAttackMenu(Graphics device)
        {
            menu.DrawMenu(device);
            if (menu.isActive)
            {
                menu.cursor.DrawCursor(device);
            }
        }

        public void Dim()
        {
            menu.Deactivate(new Bitmap("TechniqueMenuBoxDim.png"));
        }

        public void Activate()
        {
            menu.Activate(new Bitmap("TechniqueMenuBox.png"));
        }

        //Update works on the techList and the menu
        public void Update(List<Technique> newTechList)
        {
            List<string> newTechNameList = new List<string>();
            techList = newTechList;
            for(int x=0; x<techList.Count;x++)
            {
                newTechNameList.Add(techList[x].name);
            }
            menu.Update(newTechNameList);
        }

        public Technique Select()
        {
            return techList[menu.cursor.POS];
        }
    }

    class TargetMenu
    {
        public Menu menu;
        public List<Unit> units;
        public Point location;
        private List<int> offset;

        public TargetMenu()
        {
            List<string> targetNameList = new List<string>();
            offset = new List<int>();
            this.location = new Point(145, 255);


            menu = new Menu(targetNameList, this.location);

        }

        public void DrawTargetMenu(Graphics device)
        {
            menu.DrawMenu(device);
            if (menu.isActive)
            {
                menu.cursor.DrawCursor(device);
            }
        }

        public void Dim()
        {
            menu.Deactivate(new Bitmap("TargetMenuBoxDim.png"));
        }

        public void Activate()
        {
            menu.Activate(new Bitmap("TargetMenuBox.png"));

        }

       // public Monster Select()


        public void Update(List<Unit> newTargets)
        {
            List<string> targetNameList = new List<string>();
            for (int x = 0; x < newTargets.Count; x++)
            {

                if (!newTargets[x].isDead)
                {

                    targetNameList.Add(newTargets[x].name);

                }

                else
                {
                    offset.Add(x);
                }

            }
            menu.Update(targetNameList);
        }
    }

    class Combat
    {
        public List<Unit> turnOrder;
        public FlavourText flavourText;
        public Turn turn;
        private Image dimage, image;

        public Combat(List<Hero> heroList, List<Unit> monsterList, FlavourText flavourText, PictureBox GraphicalIF, Image background)
        {
            image = new Bitmap(GraphicalIF.Width, GraphicalIF.Height);
            monsterList = PreMonsterCombatBuff(monsterList);
            heroList = PreHeroCombatBuff(heroList);
            TurnOrder(heroList, monsterList);
            Target();
            dimage = background;
            this.flavourText = flavourText;
            Graphics device;
            device = Graphics.FromImage(image);


            for (int x = 0; x < turnOrder.Count; x++)
            {
                if (!turnOrder[x].isDead)
                {
                    turnOrder[x].SetStatus(Status.Attacking);
                    StartTurn(x);


                    device.DrawImage(dimage, new Point(0, 0));
                    this.flavourText.DrawFlavourText(device);
                    for (int y = 0; y < turnOrder.Count; y++)
                    {
                        turnOrder[y].DrawUnit(device);
                    }
                    GraphicalIF.Image = image;
                    GraphicalIF.Refresh();
                    System.Threading.Thread.Sleep(2000);
                    turnOrder[x].SetStatus(Status.OK);
                    for (int y = 0; y < turn.targets.Count; y++)
                    {
                        turnOrder[turn.targets[y]].SetStatus(Status.OK);
                    }

                }
                else
                {
                    turnOrder[x].SetStatus(Status.Dead);
                }
            }

            PostCombatReset();


        }

        public void StartTurn(int x)
        {
            turn = new Turn(turnOrder, x, flavourText);

            for(int y=0; y<turn.targets.Count;y++)
            {
                turnOrder[turn.targets[y]].SetStatus(Status.Defending);
            }

            //turnOrder[x].battleSprite.status = Status.Attacking;
            flavourText = turn.flavourText;
            //string str = String.Format("{0} targets {1}", turnOrder[x].name, turn.combatants[turn.target].name);
            //flavourText.AddLine(str);

            turnOrder = turn.combatants;
            //str = String.Format("Has {0} Hp left", turn.combatants[turn.target].HP);
            //flavourText.AddLine(str);
        }

        private void TurnOrder(List<Hero> heroList, List<Unit> monsterList)
        {
            turnOrder = new List<Unit>();

            for (int y = 10; y > 0; y--)
            {
                for (int x = 0; (x < heroList.Count || x < monsterList.Count); x++)
                {

                    if (x < heroList.Count)
                    {
                        if (heroList[x].SPD == y)
                        {
                            turnOrder.Add(heroList[x]);
                        }
                    }

                    if (x < monsterList.Count)
                    {
                        if (monsterList[x].SPD == y)
                        {
                            turnOrder.Add(monsterList[x]);
                        }
                    }
                }
            }

            for (int x = 0; x < turnOrder.Count; x++)
            {
                //turnOrder[x].Attack();
            }

        }

        private void Target()
        {
            bool hasTargeted;

            for (int x = 0; x < turnOrder.Count; x++)
            {
                if (!turnOrder[x].isFriendly)
                {
                    
                }

            }

            for (int x = 0; x < turnOrder.Count; x++)
            {
                hasTargeted = false;
                for (int y = 0; y < turnOrder.Count; y++)
                {
                    if (turnOrder[x].target == turnOrder[y].POS+turnOrder[y].offset && turnOrder[x].isFriendly!=turnOrder[y].isFriendly && !hasTargeted)
                    {
                        turnOrder[x].target = y;
                        hasTargeted = true;
                    }
                }
            }
        }

        private List<Unit> PreMonsterCombatBuff(List<Unit> unitList)
        {
            for (int x = 0; x < unitList.Count; x++)
            {
                unitList[x] = unitList[x].cA.CombatPrebuff(unitList[x]);
            }
            return unitList;
        }

        private List<Hero> PreHeroCombatBuff(List<Hero> unitList)
        {
            for (int x = 0; x < unitList.Count; x++)
            {
                unitList[x] = unitList[x].cA.CombatHeroPreBuff(unitList[x]);
            }
            return unitList;
        }

        private void PostCombatReset()
        {
            for (int x = 0; x < turnOrder.Count; x++)
            {
                turnOrder[x].ResetStats();
            }
        }
    }

    class Turn
    {
        public FlavourText flavourText;
        public Unit unit;
        public int target, currentCombatant;
        public List<int> targets;
        public List<Unit> combatants;


        public Turn(List<Unit> combatants, int currentCombatant, FlavourText flavText)
        {
            flavourText = flavText;
            this.currentCombatant = currentCombatant;
            this.combatants = combatants;
            target = combatants[currentCombatant].target;
            if (!combatants[currentCombatant].isDead)
            {
                Attack();
            }


            DeathCheck();
        }

        public void Attack()
        {
            //flavourText.AddLine(String.Format("{0}{1}{2}", combatants[currentCombatant].name, combatants[currentCombatant].attackList[combatants[currentCombatant].selection].flavor, combatants[target].name));
            combatants = combatants[currentCombatant].cA.Use(combatants, currentCombatant);
            targets = combatants[currentCombatant].cA.targetTOPosition;
            for (int x=0;x< combatants[currentCombatant].cA.targetTOPosition.Count;x++)
            {
                flavourText.AddLine(String.Format("{0}{1}{2}", combatants[currentCombatant].name, combatants[currentCombatant].cA.flavor, combatants[combatants[currentCombatant].cA.targetTOPosition[x]].name));
            }
            //flavourText.AddLine(String.Format("{0} takes {1} damge", combatants[target].name, combatants[currentCombatant].STR));
            //flavourText.AddLine(String.Format("{0} has {1} HP left", combatants[target].name, combatants[target].HP));
            //for (int x = 0; x < combatants.Count; x++)
            //{
            //    if (combatants[currentCombatant].isFriendly != combatants[x].isFriendly)
            //    {
            //        if (combatants[currentCombatant].target == combatants[x].POS)
            //        { 
            //            target = x;
            //        }
            //    }
            //}
            //combatants[target].HP -= combatants[currentCombatant].STR;
            //DeathCheck();
        }

        public void DeathCheck()
        {
            for (int x = 0; x < combatants.Count; x++)
            {
                if (combatants[x].HP <= 0)
                {
                    if(!combatants[x].isDead)
                    {
                         flavourText.AddLine(String.Format("{0} has died", combatants[x].name));

                    }

                    combatants[x].isDead = true;
                    combatants[x].SetStatus(Status.Dead);
                }
            }
        }


    }

    class Technique
    {
        public List<int> targetTOPosition, validTargets;
        public String name;
        public String flavor;
        public List<int> targetPositions;
        public List<bool> targetisAlly;
        public int STRBNS, SPDBNS;
        public AttackID ID;

        public Technique(AttackID ID)
        {
            this.ID = ID;
            targetPositions = new List<int>();
            targetisAlly = new List<bool>();
            validTargets = new List<int>();



            switch (ID)
            {
                case AttackID.Power:
                    name = "Power Smash";
                    STRBNS = 2;
                    SPDBNS = 0;
                    flavor = " charges a powerful blow against ";
                    break;
                case AttackID.Quick:
                    name = "Quick Slash";
                    STRBNS = 0;
                    SPDBNS = 5;
                    flavor = " dashes quickly at ";
                    break;
                case AttackID.MagicEx:
                    name = "Magic Explosion";
                    STRBNS = -3;
                    SPDBNS = 2;
                    flavor = "'s hands glow with power";
                    break;
                case AttackID.PonitBB:
                    name = "Point Blank Blast";
                    STRBNS = 2;
                    SPDBNS = 2;
                    flavor = " releases a magical blast at ";
                    break;
                case AttackID.Cancel:
                    name = "Cancel";
                    break;
                default:
                    STRBNS = 0;
                    SPDBNS = 0;
                    name = "Melancholy Strike";
                    flavor = " prepares an attack against ";
                    break;
            }

            SetValidTargets();
        }

        public void SetValidTargets()
        {
            switch (ID)
            {
                case AttackID.PonitBB:
                    validTargets.Add(0);
                    break;
                case AttackID.Cancel:
                    break;
                default:
                    for (int x = 0; x < 4; x++)
                    {
                        validTargets.Add(x);
                    }
                    break;
            }
        }

        public void Target(Unit targetUnit)
        {
            switch (ID)
            {
                case AttackID.MagicEx:
                    for (int x =0;x<4;x++)
                    {
                        targetPositions.Add(x);
                        targetisAlly.Add(targetUnit.isFriendly);
                    }
                    break;
                default:
                    targetPositions.Add(targetUnit.POS);
                    targetisAlly.Add(targetUnit.isFriendly);
                    break;
            }
        }

        public Unit CombatPrebuff(Unit unit)
        {
            unit.SPD += SPDBNS;
            unit.STR += STRBNS;
            return unit;
        }

        public Monster CombatMonsterPreBuff(Monster unit)
        {
            Monster unit2 = unit;
            unit2.SPD += SPDBNS;
            unit2.STR += STRBNS;
            return unit2;
        }

        public Hero CombatHeroPreBuff(Hero unit)
        {
            Hero unit2 = unit;
            unit2.SPD += SPDBNS;
            unit2.STR += STRBNS;
            return unit2;
        }

        public List<Unit> Use(List<Unit> units, int cc)
        {
            targetTOPosition = new List<int>();

            for(int x=0; x<units.Count;x++)
            {
                for(int y=0;y<targetPositions.Count;y++)
                {
                    if (units[x].POS ==targetPositions[y] && units[x].isFriendly ==targetisAlly[y])
                    {
                        targetTOPosition.Add(x);
                    }
                }
            }

            switch(ID)
            {
                case AttackID.MagicEx:
                    for(int x = 0; x<targetTOPosition.Count;x++)
                    {
                        units[targetTOPosition[x]].HP -= units[cc].STR;
                    }
                    break;
                case AttackID.Power:
                    units[targetTOPosition[0]].HP -= units[cc].STR;
                    break;
                default:
                    units[targetTOPosition[0]].HP -= units[cc].STR;
                    break;

            }

            return units;
        }
    }

    public enum AttackID
    {
        Power, Quick, MagicEx, none, PonitBB, Cancel
    }

    //Player Input//

    class Menu
    {
        public Cursor cursor;
        public List<String> items;
        public bool isActive;
        public Point location, boxLoc;

        Brush brush, dimBrush;
        Font font;
        int numItems;
        public Image box;

        public Menu(List<string> items, Point location)
        {
            boxLoc = location;
            location.X += 20;
            location.Y += 50;
            this.location = location;
            this.items = items;
            numItems = items.Count();
            isActive = false;
            font = new System.Drawing.Font("Helvetica", 10, FontStyle.Italic);
            brush = new SolidBrush(System.Drawing.Color.White);
            // dimFont = new System.Drawing.Font("Helvetica", 10, FontStyle.Italic);
            dimBrush = new SolidBrush(System.Drawing.Color.Blue);

        }

        public void DrawMenu(Graphics device)
        {
            device.DrawImage(box, boxLoc);
            for (int x = 0; x < numItems; x++)
            {
                Point loc;
                loc = location;
                loc.Y += 20 * x;
                if (isActive)
                {
                    device.DrawString(items[x], font, brush, loc);
                }
                else
                {
                    device.DrawString(items[x], font, dimBrush, loc);

                }
            }
        }

        public void Activate(Image backBox)
        {
            box = backBox;
            isActive = true;
            cursor = new Cursor(numItems, location);


        }

        public void Deactivate(Image backBox)
        {
            box = backBox;
            isActive = false;
        }

        public void Update(List<string> newItems)
        {
            items = newItems;
            numItems = newItems.Count;
        }
    }

    class Cursor
    {
        Image image;
        Point location;
        public int POS;
        public int numItems;

        public Cursor(int numItems, Point location)
        {
            this.numItems = numItems;
            image = new Bitmap("Cursor.png");
            location.X -= image.Width;
            this.location = location;
            POS = 0;
        }

        public void DrawCursor(Graphics device)
        {
            device.DrawImage(image, location);
        }

        public void Move(int move)
        {
            if ((POS + move < numItems) && (POS + move >= 0))
            {
                POS += move;
                location.Y += move * 20;
            }
        }


    }

    //BattleFieldDisplay//

    class FlavourText
    {
        public Point location, dlocation;
        public string fText;
        public List<String> lines;
        static int length;
        static int nlength;
        Font myFont;
        Brush myBrush;
        Image textBox;

        public FlavourText()
        {
            textBox = new Bitmap("FlavourTextBox.png");
            lines = new List<String>();
            fText = null;
            myFont = new System.Drawing.Font("Helvetica", 10, FontStyle.Italic);
            myBrush = new SolidBrush(System.Drawing.Color.Red);
            dlocation = new Point(290, 255);
            location = dlocation;
            length = 10;
        }

        public void Print()
        {
            nlength = length;
            if (lines.Count < length)
            {
                location.Y += 15 * (length - lines.Count);
                nlength = lines.Count;

            }

            for (int x = 0; x < nlength; x++)
            {
                fText += lines[(lines.Count - (nlength - x))];
                //flavourText += lines[x];
            }

        }

        public void AddLine(string str)
        {
            str = str + "\n";
            lines.Add(str);
        }

        public void DrawFlavourText(Graphics device)
        {
            Print();
            location = dlocation;
            location.X += 15;
            location.Y += 30;
            device.DrawImage(textBox, dlocation);
            device.DrawString(fText, myFont, myBrush, location);
            location = dlocation;
            fText = null;
        }
    }

    class BattleSprite
    {
        public Point location;
        public Image image;
        public int POS;
        private int battlePOS;

        public BattleSprite(int position)
        {
            this.POS = position;
            battlePOS = POS;
            FindLocation();



            //if (position == 1)
            //{
            //    this.location = new Point(50, 50);
            //}
            //else if(position ==2)
            //{
            //    this.location = new Point(100, 50);
            //}
            //else if(position ==3)
            //{
            //    this.location = new Point(200, 50);
            //}
            //else if(position ==4)
            //{
            //    this.location = new Point(350, 50);
            //}
            //else if(position == 0)
            //{
            //    this.location = new Point(20, 300);
            //}
            //else
            //{
            //    this.location = new Point(300, 300);
            //}
        }

        public void FindLocation()
        {

            switch (POS)
            {
                case 0:
                    location = new Point(150, 150);
                    break;
                case 1:
                    location = new Point(100, 125);
                    break;
                case 2:
                    location = new Point(100, 175);
                    break;
                case 3:
                    location = new Point(50, 150);
                    break;
                case 4:
                    location = new Point(450, 150);
                    break;
                case 5:
                    location = new Point(500, 125);
                    break;
                case 6:
                    location = new Point(500, 175);
                    break;
                case 7:
                    location = new Point(550, 150);
                    break;
                case 8:
                    location = new Point(300, 100);
                    break;
                case 9:
                    location = new Point(400, 100);
                    break;
                default:
                    location = new Point(300, 300);
                    break;
            }

        }

        public void DrawBattleSprite(Graphics device)
        {
            FindLocation();
            device.DrawImage(image, location);
        }

        public void DrawHealthBar(Graphics device, int HP, int MXHP)
        {
            Pen pen = new Pen(Color.Lime);
            float HPremaining;
            int thickness = 4;
            HPremaining = (float) HP / MXHP;
            int length = (int)(HPremaining * image.Width);
            Point nPoint = location;
            nPoint.X += length;
            for(int y=0;y<thickness;y++)
            {
                device.DrawLine(pen, location, nPoint);
                location.Y++;
                nPoint.Y++;
            }
            location.Y -= thickness;
        }

    }

    public enum Status
    {
        OK, Dead, Attacking, Defending
    }

    ////////////////////Units and Parties//////////////////////////////////////////////////////////////////////////////////////////////////////

    class Unit
    {
        public int BSTR, BSPD;
        public int Health, STR, POS, HP, SPD, selection, target;
        public BattleSprite battleSprite;
        public string name;
        public List<string> techNameList;
        public List<Technique> techList;
        public bool isFriendly, isDead;
        public int offset;
        public Technique cA;

        public Unit(int POS, string name)
        {
            this.name = name;
            Health = 10;
            STR = 2;
            SPD = 2;
            BSPD = 2;
            BSTR = 2;
            this.POS = POS;
            selection = -1;
            battleSprite = new BattleSprite(POS);
            SetStatus(Status.OK);
            offset = 0;
            isFriendly = true;
        }

        public void DrawUnit(Graphics device)
        {
            battleSprite.DrawBattleSprite(device);
            if (!isDead)
            {
                battleSprite.DrawHealthBar(device, HP, Health);
            }
        }

        public void SetStatus(Status status)
        {
            string filename;
            switch (status)
            {
                case Status.OK:
                    filename = name + ".png";
                    battleSprite.image = new Bitmap(@filename);
                    battleSprite.POS = POS;
                    if(!isFriendly) { battleSprite.POS += 4; }
                    break;
                case Status.Defending:
                    filename = name + "Defend.png";
                    battleSprite.image = new Bitmap(@filename);
                    if(isFriendly)
                    {
                        //battleSprite.POS = 8;
                    }
                    else
                    {
                       // battleSprite.POS = 9;
                    }
                    break;
                case Status.Attacking:
                    filename = name + "Attack.png";
                    battleSprite.image = new Bitmap(@filename);
                    if (isFriendly)
                    {
                        battleSprite.POS = 8;
                    }
                    else
                    {
                        battleSprite.POS = 9;
                    }
                    break;
                case Status.Dead:
                    filename = name + "Dead.png";
                    battleSprite.image = new Bitmap(@filename);
                    battleSprite.POS = POS;
                    if (!isFriendly) { battleSprite.POS += 4; }
                    break;
            }
        }

        public void ResetStats()
        {
            STR = BSTR;
            SPD = BSPD;
        }

        public void UseTechnique(List<Unit> units, int x)
        {
            units[x].techList[units[x].selection].Use(units, x);
        }
    }

    class Hero : Unit
    {
        public Technique cancel;
        public List<Technique> validTechs;

        public Hero(string ID, int POS) : base(POS, ID)
        {
            cancel = new Technique(AttackID.Cancel);

            Image image;
            techList = new List<Technique>();

            techNameList = new List<String>();
            isFriendly = true;
            switch (ID)
            {
                case "Powerdurk":
                    // name = "Powerdurk";

                    techList.Add(new Technique(AttackID.Power));
                    techList.Add(new Technique(AttackID.Quick));
                    techList.Add(new Technique(AttackID.MagicEx));
                    techList.Add(new Technique(AttackID.PonitBB));
                    SPD = 5;
                    STR = 5;
                    BSPD = 5;
                    BSTR = 5;
                    break;
                default:
                    image = new Bitmap("Battler.png");
                    name = "Battler";
                    techList.Add(new Technique(AttackID.Power));
                    techList.Add(new Technique(AttackID.Quick));
                    SPD = 3;
                    STR = 4;
                    BSPD = 3;
                    BSTR = 4;
                    break;
            }
            //battleSprite = new BattleSprite(POS, image);

            for (int x = 0; x < techList.Count; x++)
            {
                techNameList.Add(techList[x].name);
            }
            SetStatus(Status.OK);
        }

    }

    class Monster : Unit
    {
        public Monster(int POS, MonsterID monsterID) : base(POS, Enum.GetName(typeof(MonsterID), monsterID))
        {
            selection = 0;
            techList = new List<RPG_game2.Technique>();
            Image image;
            this.name = Enum.GetName(typeof(MonsterID), monsterID);
            Unit defaultTarget = new Unit(0, "target0");

            switch (monsterID)
            {
                case MonsterID.Demon:
                    SPD = 4;
                    STR = 1;
                    BSPD = 4;
                    BSTR = 1;
                    techList.Add(new Technique(AttackID.none));
                    break;
                case MonsterID.Demon2:
                    SPD = 2;
                    STR = 3;
                    BSPD = 2;
                    BSTR = 3;
                    techList.Add(new Technique(AttackID.none));
                    break;
                default:
                    SPD = 2;
                    STR = 2;
                    BSPD = 2;
                    BSTR = 2;
                    techList.Add(new Technique(AttackID.none));
                    break;
            }
            techList[0].Target(defaultTarget);
            cA = techList[0];
            isFriendly = false;
            battleSprite.POS = POS + 4;
            SetStatus(Status.OK);
            //battleSprite = new BattleSprite(this.POS, image);

        }
    }

    public enum MonsterID
    {
        Demon, Demon2, cancel
    }

    class PlayerParty
    {
        Image image;
        public Point location;

        public WorldMapSprite partySprite;
        public List<Hero> heros;

        public PlayerParty(Point location, int ID)
        {

            this.location = location;

            image = new Bitmap("player Party Sprite.png");
            partySprite = new WorldMapSprite(image, location);
            image = new Bitmap("Powerdurk.png");
            heros = new List<Hero>();
            heros.Add(new Hero("Powerdurk", 0));
            heros.Add(new Hero("Battler", 1));
        }
        public void Move(int x, int y)
        {
            location.X += x;
            location.Y += y;
            partySprite.Move(x, y);
        }
    }


}