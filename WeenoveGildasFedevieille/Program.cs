﻿using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using static Party;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Party
{
    /*____________________________________________________________*/
    /*__________________________CONSTANTES________________________*/
    /*____________________________________________________________*/

    public const int TYPE_MONSTER = 0;
    public const int TYPE_MY_HERO = 1;
    public const int TYPE_OP_HERO = 2;

    public const int THREAT_FOR_ME = 1;
    public const int THREAT_FOR_OPP = 2;

    public const int MY_HERO1 = 0;
    public const int MY_HERO2 = 1;
    public const int MY_HERO3 = 2;

    private const int MIDDLE_MAGNITUDE = 9897;

    public static int EXPLORATION_MAGNITUDE = 8200;
    public const double PIXEL_SCOPE = 2200;
    public static double RADIAN_SCOPE = Math.Asin(PIXEL_SCOPE / EXPLORATION_MAGNITUDE);
    public static double EXPLORATION_PHASE_SHIFT = 2 * Math.Asin((HERO_SPEED / 2) / EXPLORATION_MAGNITUDE);
    public const double HERO_SPEED = 800;
    public static double EXPLORATION_RADIAN_WIDTH = Math.PI / 2 - 2 * RADIAN_SCOPE;
    public static double EXPLORATOIN_THIRD_RADIAN_WIDTH = EXPLORATION_RADIAN_WIDTH / 3;

    public const int ATTACK_TURN = 90;

    /*____________________________________________________________*/
    /*__________________________ATTRIBUTS_________________________*/
    /*____________________________________________________________*/

    static protected int MyMana { get; set; }
    static protected int MyHealth { get; set; }
    static protected int OppMana { get; set; }
    static protected int OppHealth { get; set; }
    static protected int Turn { get; set; }
    static protected bool OpponentUseControlOnMyDenfenders { get; set; }
    static protected Complex MyBase { get; set; }
    static protected Complex OppBase { get; set; }

    static protected List<Monster>? Monsters;
    static protected List<MyHero>? MyHeroes;
    static protected List<OppHero>? OppHeroes;

    static protected List<int?>? UrgentTargetRoute;

    static protected int LastUrgentTargetUpdate = 0;

    /*____________________________________________________________*/
    /*___________________________METHODES_________________________*/
    /*____________________________________________________________*/

    public void AddTurn()
    {
        Party.Turn++;

        if (Party.Turn == ATTACK_TURN)
        {
            UpdateExplorationMagnitude(6000);
        }
    }

    public static void UpdateExplorationMagnitude(int explorationMagnitude)
    {
        EXPLORATION_MAGNITUDE = explorationMagnitude;
        RADIAN_SCOPE = Math.Asin(PIXEL_SCOPE / EXPLORATION_MAGNITUDE);
        EXPLORATION_PHASE_SHIFT = 2 * Math.Asin((HERO_SPEED / 2) / EXPLORATION_MAGNITUDE);
        EXPLORATION_RADIAN_WIDTH = Math.PI / 2 - 2 * RADIAN_SCOPE;
        EXPLORATOIN_THIRD_RADIAN_WIDTH = EXPLORATION_RADIAN_WIDTH / 3;
    }

    public static void UpdateDenfenseTargetList()
    {
        for (int i = 0; i < Party.Monsters.Count; i++)
        {
            if (Party.Monsters[i].IsThreatForOppenent())
            {
                Party.Monsters.RemoveAt(i);
                i--;
            }
        }

        Party.Monsters.Sort();
    }

    public static void UpdateUrgentTarget()
    {
        Party.UpdateDenfenseTargetList();

        Party.UrgentTargetRoute = new List<int?> { null, null, null };

        List<int> freeHeroes;

        if (Party.Turn > ATTACK_TURN)
        {
            freeHeroes = new List<int> { 1, 2 };
        }
        else
        {
            freeHeroes = new List<int> { 0, 1, 2 };
        }

        for (int i = 0; i < Party.Monsters.Count && i < 3 && Party.Monsters[i].DistanceToMyBase < 7200; i++)
        {
            int? nearestHero = Party.Monsters[i].GetNearestHeroInTheList(freeHeroes);

            if (nearestHero != null)
            {
                Party.UrgentTargetRoute[(int)nearestHero] = i;

                freeHeroes.RemoveAt(freeHeroes.IndexOf((int)nearestHero));
            }

            if (Party.Monsters[i].ShieldLife > 0)
            {
                int? secondNearestHero = Party.Monsters[i].GetNearestHeroInTheList(freeHeroes);

                if (secondNearestHero != null)
                {
                    Party.UrgentTargetRoute[(int)secondNearestHero] = i;

                    freeHeroes.RemoveAt(freeHeroes.IndexOf((int)secondNearestHero));

                    i++;
                }
            }
        }
    }

    public static int? GetUrgentTarget(int hero)
    {
        if (Party.LastUrgentTargetUpdate != Party.Turn)
        {
            Party.UpdateUrgentTarget();
            Party.LastUrgentTargetUpdate = Party.Turn;
        }

        return Party.UrgentTargetRoute[hero];
    }

    /*____________________________________________________________*/
    /*____________________________ENTITY__________________________*/
    /*____________________________________________________________*/

    public class Entity
    {
        public int Id;
        public Complex Position { get; set; }
        public int ShieldLife;
        public bool IsControlled;

        public Entity(int id, Complex position, int shieldLife, bool isControlled)
        {
            this.Id = id;
            this.Position = position;
            this.ShieldLife = shieldLife;
            this.IsControlled = isControlled;
        }

        public int DistanceTo(Entity entity)
        {
            return (int)Complex.Subtract(this.Position, entity.Position).Magnitude;
        }

        public int DistanceTo(Complex position)
        {
            return (int)Complex.Subtract(this.Position, position).Magnitude;
        }
    }

    /*____________________________________________________________*/
    /*___________________________MONSTER__________________________*/
    /*____________________________________________________________*/

    public class Monster : Entity, IComparable
    {
        public int Health { get; }
        public double DistanceToMyBase { get; }
        public double DistanceToOppenentBase { get; }
        private int ThreatFor;

        public Monster(int id, Complex position, int shieldLife, bool isControlled, int health, int threatFor) : base(id, position, shieldLife, isControlled)
        {
            this.Health = health;
            this.DistanceToMyBase = this.DistanceTo(Party.MyBase);
            this.DistanceToOppenentBase = this.DistanceTo(Party.OppBase);
            this.ThreatFor = threatFor;
        }

        public bool IsThreatForMe()
        {
            return (this.ThreatFor == THREAT_FOR_ME);
        }

        public bool IsThreatForOppenent()
        {
            return (this.ThreatFor == THREAT_FOR_OPP);
        }

        public int CompareTo(object? obj)
        {
            if ((this.IsThreatForMe() && !((Monster)obj).IsThreatForMe()) || this.DistanceToMyBase < ((Monster)obj).DistanceToMyBase)
            {
                return -1;
            }
            else if ((!this.IsThreatForMe() && ((Monster)obj).IsThreatForMe()) || this.DistanceToMyBase > ((Monster)obj).DistanceToMyBase)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int? GetNearestHeroInTheList(List<int> heroesIndex)
        {
            if (heroesIndex.Count > 0)
            {
                int nearestHeroIndex = heroesIndex[0];

                for (int i = 1; i < heroesIndex.Count; i++)
                {
                    if (this.DistanceTo(Party.MyHeroes[heroesIndex[i]]) < this.DistanceTo(Party.MyHeroes[heroesIndex[nearestHeroIndex]]))
                    {
                        nearestHeroIndex = heroesIndex[i];
                    }
                }

                return nearestHeroIndex;
            }

            return null;
        }

        public bool CanBeWinded()
        {
            foreach (OppHero oppHero in Party.OppHeroes)
            {
                if (this.DistanceTo(oppHero) <= 1280 + 400)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /*____________________________________________________________*/
    /*____________________________MYHERO__________________________*/
    /*____________________________________________________________*/

    public abstract class MyHero : Entity
    {
        public Complex AlterVector { get; set; }

        public MyHero(int id, Complex position, int shieldLife, bool isControlled) : base(id, position, shieldLife, isControlled)
        {

        }
        public virtual void Update(Complex position, int shieldLife, bool isControlled)
        {
            this.Position = position;
            this.ShieldLife = shieldLife;
            this.IsControlled = isControlled;
        }

        public abstract void Action();
        protected void GoTo(Complex position)
        {
            Console.WriteLine($"MOVE {(int)position.Real} {(int)position.Imaginary}");
        }

        protected void Wait()
        {
            Console.WriteLine("WAIT");
        }
        protected void Wind(Complex direction)
        {
            Console.WriteLine($"SPELL WIND {(int)direction.Real} {(int)direction.Imaginary}");
            Party.MyMana -= 10;
        }

        protected void Shield(int id)
        {
            Console.WriteLine($"SPELL SHIELD {(int)id}");
            Party.MyMana -= 10;
        }
        protected void Control(int id, Complex position)
        {
            Console.WriteLine($"SPELL CONTROL {(int)id} {(int)position.Real} {(int)position.Imaginary}");
            Party.MyMana -= 10;
        }

        protected void WindTo(Complex position)
        {
            Complex relativeDirection = Complex.Add(this.Position, position);
            this.Wind(relativeDirection);
        }

        protected void GoTo(Monster monster)
        {
            this.GoTo(monster.Position);
        }

        protected void DefenseExplore()
        {
            Complex position = Complex.Add(Party.MyBase, this.AlterVector);

            this.GoTo(position);
        }

        protected int? GetTargetInScope()
        {
            for (int i = 0; i < Party.Monsters.Count; i++)
            {
                double relativePhase = Complex.Subtract(Party.Monsters[i].Position, Party.MyBase).Phase;

                if (Party.Monsters[i].DistanceToMyBase >= 7200 && Math.Abs(relativePhase - this.AlterVector.Phase) < RADIAN_SCOPE)
                {
                    return i;
                }
            }

            return null;
        }

        protected bool MustDefenseWind(int target)
        {
            return (Party.Monsters![target].ShieldLife == 0 && Party.Monsters[target].DistanceTo(this) <= 1280 && Party.MyMana >= 10 &&
                    (Party.Monsters[target].DistanceToMyBase < 1100
                    || (Party.Monsters[target].DistanceToMyBase < 2600 && Party.Monsters[target].CanBeWinded())
                    || (Party.Turn > 90 && Party.Monsters[target].DistanceToMyBase <= 5000)));
        }
    }

    /*____________________________________________________________*/
    /*____________________________MYHERO1_________________________*/
    /*____________________________________________________________*/

    public class MyHero1 : MyHero
    {
        private double PhaseShift;
        private int AttackExplorationMagnitude;
        private double MinPhase;
        private double MaxPhase;
        private bool MustJoinShieldPoint;
        private double ShieldPointPhase;

        public MyHero1(int id, Complex position, int shieldLife, bool isControlled) : base(id, position, shieldLife, isControlled)
        {
            this.AttackExplorationMagnitude = 6000;

            this.PhaseShift = 2 * Math.Asin((HERO_SPEED / 2) / this.AttackExplorationMagnitude);

            double radianScope = Math.Asin(PIXEL_SCOPE / this.AttackExplorationMagnitude);

            if (Party.MyBase.Real == 0)
            {
                this.AlterVector = Complex.FromPolarCoordinates(EXPLORATION_MAGNITUDE, Math.PI / 4);
                this.MinPhase = -Math.PI + radianScope;
                this.MaxPhase = (-Math.PI / 2) - radianScope;
                this.ShieldPointPhase = -3 * Math.PI / 4;
            }
            else
            {
                this.AlterVector = Complex.FromPolarCoordinates(EXPLORATION_MAGNITUDE, -3 * Math.PI / 4);
                this.MinPhase = radianScope;
                this.MaxPhase = (Math.PI / 2) - radianScope;
                this.ShieldPointPhase = Math.PI / 4;
            }

            this.MustJoinShieldPoint = false;
        }

        public override void Action()
        {
            if (Party.Turn > ATTACK_TURN)
            {
                this.AttackAction();
            }
            else
            {
                this.DefenseAction();
            }
        }

        private void AttackAction()
        {
            int? id;

            if ((id = this.MustShield()) != null)
            {
                this.Shield((int)id);
                this.MustJoinShieldPoint = false;
            }
            else if (this.MustJoinShieldPoint)
            {
                Complex position = Complex.Add(Party.OppBase, Complex.FromPolarCoordinates(2500, this.ShieldPointPhase));

                if (this.Position.Real == (int)position.Real && this.Position.Imaginary == (int)position.Imaginary)
                {
                    this.MustJoinShieldPoint = false;
                    this.AttackExplore();
                }
                else
                {
                    this.GoTo(position);
                }
            }
            else if (this.MustAttackWind())
            {
                this.MustJoinShieldPoint = true;
                this.Wind(Party.OppBase);
            }
            else if ((id = this.MustControl()) != null)
            {
                this.Control((int)id, Party.OppBase);
            }
            else
            {
                this.AttackExplore();
            }
        }

        private void DefenseAction()
        {
            int? urgentTarget = Party.GetUrgentTarget(MY_HERO1);

            if (urgentTarget != null)
            {
                if (this.MustDefenseWind((int)urgentTarget))
                {
                    //!!! optimiser
                    this.Wind(Party.OppBase);
                }
                else
                {
                    this.GoTo(Party.Monsters[(int)urgentTarget]);
                }
            }
            else
            {
                int? targetInMyScope = this.GetTargetInScope();

                if (targetInMyScope != null)
                {
                    this.GoTo(Party.Monsters[(int)targetInMyScope]);
                }
                else
                {
                    this.DefenseExplore();
                }
            }
        }

        private void AttackExplore()
        {
            Complex position = Complex.Add(Party.OppBase, this.AlterVector);

            this.GoTo(position);

            this.AlterVector = Complex.FromPolarCoordinates(this.AttackExplorationMagnitude, this.AlterVector.Phase + this.PhaseShift);

            if (this.AlterVector.Phase > this.MaxPhase)
            {
                this.AlterVector = Complex.FromPolarCoordinates(this.AttackExplorationMagnitude, this.MaxPhase);
                this.PhaseShift = -this.PhaseShift;
            }
            else if (this.AlterVector.Phase < this.MinPhase)
            {
                this.AlterVector = Complex.FromPolarCoordinates(this.AttackExplorationMagnitude, this.MinPhase);
                this.PhaseShift = -this.PhaseShift;
            }
        }

        private List<Monster> GetScope()
        {
            List<Monster> scope = new List<Monster>();

            foreach (Monster monster in Party.Monsters!)
            {
                if (this.DistanceTo(monster) <= 2200 && monster.DistanceToOppenentBase < MIDDLE_MAGNITUDE)
                {
                    scope.Add(monster);
                }
            }

            return scope;
        }

        private bool MustAttackWind()
        {
            if (Party.MyMana < 10)
            {
                return false;
            }

            foreach (Monster monster in this.GetScope())
            {
                if (this.DistanceTo(monster) <= 1280 && monster.Health > 16 && monster.ShieldLife == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int? MustShield()
        {
            if (Party.MyMana < 10)
            {
                return null;
            }

            foreach (Monster monster in this.GetScope())
            {
                double remainsLifes = monster.Health - 2 * (monster.DistanceToOppenentBase / 400);

                if (monster.DistanceToOppenentBase < 4400 && remainsLifes > 0 && monster.ShieldLife == 0)
                {
                    return monster.Id;
                }
            }

            return null;
        }

        private int? MustControl()
        {
            if (Party.MyMana < 10)
            {
                return null;
            }

            foreach (Monster monster in this.GetScope())
            {
                if (monster.DistanceToOppenentBase > 7280 && monster.Health > 16 && monster.ShieldLife == 0 && !monster.IsThreatForOppenent())
                {
                    return monster.Id;
                }
            }

            return null;
        }
    }

    /*____________________________________________________________*/
    /*____________________________MYHERO2_________________________*/
    /*____________________________________________________________*/

    public class MyHero2 : MyHero
    {
        public MyHero2(int id, Complex position, int shieldLife, bool isControlled) : base(id, position, shieldLife, isControlled)
        {
            if (Party.MyBase.Real == 0)
            {
                this.AlterVector = Complex.FromPolarCoordinates(EXPLORATION_MAGNITUDE, RADIAN_SCOPE);
            }
            else
            {
                this.AlterVector = Complex.FromPolarCoordinates(EXPLORATION_MAGNITUDE, -Math.PI + RADIAN_SCOPE);
            }
        }
        public override void Update(Complex position, int shieldLife, bool isControlled)
        {
            this.Position = position;
            this.ShieldLife = shieldLife;
            this.IsControlled = isControlled;

            if (Party.Turn > ATTACK_TURN && !Party.OpponentUseControlOnMyDenfenders && this.IsControlled)
            {
                Party.OpponentUseControlOnMyDenfenders = true;
            }
        }

        public override void Action()
        {
            int? urgentTarget = Party.GetUrgentTarget(MY_HERO2);

            if (urgentTarget != null)
            {
                if (this.MustDefenseWind((int)urgentTarget))
                {
                    //!!! optimiser
                    this.Wind(Party.OppBase);
                }
                else
                {
                    this.GoTo(Party.Monsters[(int)urgentTarget]);
                }
            }
            else
            {
                int? targetInMyScope = this.GetTargetInScope();

                if (targetInMyScope != null)
                {
                    this.GoTo(Party.Monsters[(int)targetInMyScope]);
                }
                else
                {
                    this.DefenseExplore();
                }
            }
        }
    }

    /*____________________________________________________________*/
    /*____________________________MYHERO3_________________________*/
    /*____________________________________________________________*/

    public class MyHero3 : MyHero
    {
        public MyHero3(int id, Complex position, int shieldLife, bool isControlled) : base(id, position, shieldLife, isControlled)
        {
            if (Party.MyBase.Real == 0)
            {
                this.AlterVector = Complex.FromPolarCoordinates(EXPLORATION_MAGNITUDE, (Math.PI / 2 - RADIAN_SCOPE));
            }
            else
            {
                this.AlterVector = Complex.FromPolarCoordinates(EXPLORATION_MAGNITUDE, -Math.PI / 2 - RADIAN_SCOPE);
            }
        }

        public override void Action()
        {
            int? urgentTarget = Party.GetUrgentTarget(MY_HERO3);

            if (urgentTarget != null)
            {
                if (this.MustDefenseWind((int)urgentTarget))
                {
                    this.Wind(Party.OppBase);
                }
                else
                {
                    this.GoTo(Party.Monsters[(int)urgentTarget]);
                }
            }
            else
            {
                int? targetInMyScope = this.GetTargetInScope();

                if (targetInMyScope != null)
                {
                    this.GoTo(Party.Monsters[(int)targetInMyScope]);
                }
                else
                {
                    this.DefenseExplore();
                }
            }
        }
    }

    /*____________________________________________________________*/
    /*___________________________OPPHERO__________________________*/
    /*____________________________________________________________*/
    public class OppHero : Entity
    {
        public OppHero(int id, Complex position, int shieldLife, bool isControlled) : base(id, position, shieldLife, isControlled)
        {

        }
    }

    /*____________________________________________________________*/
    /*_____________________________MAIN___________________________*/
    /*____________________________________________________________*/

    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');

        int baseX = int.Parse(inputs[0]);
        int baseY = int.Parse(inputs[1]);

        Party.MyBase = new Complex(baseX, baseY);

        if (Party.MyBase.Real == 0)
        {
            Party.OppBase = new Complex(17630, 9000);
        }
        else
        {
            Party.OppBase = new Complex(0, 0);
        }

        int heroesPerPlayer = int.Parse(Console.ReadLine());

        Party.MyHeroes = new List<MyHero>(heroesPerPlayer);

        //init party
        Party.Turn = 0;

        // game loop
        while (true)
        {
            Party.Turn++;

            inputs = Console.ReadLine().Split(' ');
            Party.MyHealth = int.Parse(inputs[0]);
            Party.MyMana = int.Parse(inputs[1]);

            inputs = Console.ReadLine().Split(' ');
            Party.OppHealth = int.Parse(inputs[0]);
            Party.OppMana = int.Parse(inputs[1]);

            int entityCount = int.Parse(Console.ReadLine());

            Party.OppHeroes = new List<OppHero>(entityCount);
            Party.Monsters = new List<Monster>(entityCount);

            int hero = 0;

            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int id = int.Parse(inputs[0]); // Unique identifier
                int type = int.Parse(inputs[1]); // 0=monster, 1=your hero, 2=opponent hero
                int x = int.Parse(inputs[2]); // Position of this entity
                int y = int.Parse(inputs[3]);
                int shieldLife = int.Parse(inputs[4]); // Ignore for this league; Count down until shield spell fades
                int isControlled = int.Parse(inputs[5]); // Ignore for this league; Equals 1 when this entity is under a control spell
                int health = int.Parse(inputs[6]); // Remaining health of this monster
                int vx = int.Parse(inputs[7]); // Trajectory of this monster
                int vy = int.Parse(inputs[8]);
                int nearBase = int.Parse(inputs[9]); // 0=monster with no urgentTarget yet, 1=monster targeting a base
                int threatFor = int.Parse(inputs[10]); // Given this monster's trajectory, is it a threat to 1=your base, 2=your opponent's base, 0=neither

                switch (type)
                {
                    case TYPE_MONSTER:
                        Party.Monsters.Add(new Monster(id, new Complex(x, y), shieldLife, (isControlled == 1), health, threatFor));
                        break;
                    case TYPE_MY_HERO:

                        if (Party.Turn != 1)
                        {
                            Party.MyHeroes[hero].Update(new Complex(x, y), shieldLife, (isControlled == 1));
                        }
                        else
                        {
                            switch (hero)
                            {
                                case MY_HERO1:
                                    Party.MyHeroes.Add(new MyHero1(id, new Complex(x, y), shieldLife, (isControlled == 1)));
                                    break;
                                case MY_HERO2:
                                    Party.MyHeroes.Add(new MyHero2(id, new Complex(x, y), shieldLife, (isControlled == 1)));
                                    break;
                                case MY_HERO3:
                                    Party.MyHeroes.Add(new MyHero3(id, new Complex(x, y), shieldLife, (isControlled == 1)));
                                    break;
                            }
                        }

                        hero++;

                        break;
                    case TYPE_OP_HERO:
                        Party.OppHeroes.Add(new OppHero(id, new Complex(x, y), shieldLife, (isControlled == 1)));
                        break;
                }
            }

            for (int i = 0; i < heroesPerPlayer; i++)
            {
                Party.MyHeroes[i].Action();
            }
            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
        }
    }
}