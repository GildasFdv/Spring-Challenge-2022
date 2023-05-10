using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using static Party;

class Party
{
    /*____________________________________________________________*/
    /*__________________________CONSTANTES________________________*/
    /*____________________________________________________________*/

    private const int TYPE_MONSTER = 0;
    private const int TYPE_MY_HERO = 1;
    private const int TYPE_OP_HERO = 2;

    private const int THREAT_FOR_ME = 1;
    private const int THREAT_FOR_OPP = 2;

    private const int MY_HERO1 = 0;
    private const int MY_HERO2 = 1;
    private const int MY_HERO3 = 2;

    private const int MIDDLE_MAGNITUDE = 9897;

    private static int EXPLORATION_MAGNITUDE = 6000;
    private const double PIXEL_SCOPE = 2200;
    private static double RADIAN_SCOPE = Math.Asin(PIXEL_SCOPE / EXPLORATION_MAGNITUDE);
    private const double HERO_SPEED = 800;

    private const int ATTACK_TURN = 90;

    /*____________________________________________________________*/
    /*__________________________ATTRIBUTS_________________________*/
    /*____________________________________________________________*/

    static private int MyMana { get; set; }
    static private int MyHealth;
    static private int OppMana;
    static private int OppHealth;
    static private int Turn { get; set; }
    static private bool OpponentUseControlOnMyDefenders { get; set; }
    static private bool OpponentUseControlOnAttacker { get; set; }
    static private Complex MyBase { get; set; }
    static private Complex OppBase { get; set; }

    static private List<Monster>? Monsters;
    static private List<MyHero>? MyHeroes;
    static private List<OppHero>? OppHeroes;

    static private List<int?>? UrgentTargetRoutes;

    static private int LastUrgentTargetUpdate = 0;

    /*____________________________________________________________*/
    /*___________________________METHODES_________________________*/
    /*____________________________________________________________*/

    private static void AddTurn()
    {
        Party.Turn++;

        if (Party.Turn == ATTACK_TURN)
        {
            Party.UpdateExplorationMagnitude(6000);
        }
    }

    private static void UpdateExplorationMagnitude(int explorationMagnitude)
    {
        EXPLORATION_MAGNITUDE = explorationMagnitude;
        RADIAN_SCOPE = Math.Asin(PIXEL_SCOPE / EXPLORATION_MAGNITUDE);
    }

    private static void UpdateDenfenseTargetList()
    {
        for (int i = 0; i < Party.Monsters!.Count; i++)
        {
            if (Party.Monsters[i].IsThreatForOppenent())
            {
                Party.Monsters.RemoveAt(i);
                i--;
            }
        }

        Party.Monsters.Sort();
    }

    private static void UpdateUrgentTarget()
    {
        Party.UpdateDenfenseTargetList();

        Party.UrgentTargetRoutes = new List<int?> { null, null, null };

        List<int> freeHeroes = Party.Turn > ATTACK_TURN ? new List<int> { 1, 2 } : new List<int> { 0, 1, 2 };

        for (int i = 0; i < Party.Monsters!.Count && i < 3 && Party.Monsters[i].DistanceToMyBase < 7200; i++)
        {
            int? nearestHero = Party.Monsters[i].GetNearestHeroInTheList(freeHeroes);

            if (nearestHero != null)
            {
                Party.UrgentTargetRoutes[(int)nearestHero] = i;

                freeHeroes.RemoveAt(freeHeroes.IndexOf((int)nearestHero));
            }

            if (Party.Monsters[i].ShieldLife > 0)
            {
                int? secondNearestHero = Party.Monsters[i].GetNearestHeroInTheList(freeHeroes);

                if (secondNearestHero != null)
                {
                    Party.UrgentTargetRoutes[(int)secondNearestHero] = i;

                    freeHeroes.RemoveAt(freeHeroes.IndexOf((int)secondNearestHero));

                    i++;
                }
            }
        }
    }

    private static int? GetUrgentTarget(int hero)
    {
        if (Party.LastUrgentTargetUpdate != Party.Turn)
        {
            Party.UpdateUrgentTarget();
            Party.LastUrgentTargetUpdate = Party.Turn;
        }

        return Party.UrgentTargetRoutes![hero];
    }

    /*____________________________________________________________*/
    /*____________________________ENTITY__________________________*/
    /*____________________________________________________________*/

    public class Entity
    {
        public int Id;
        public Complex Position { get; set; }
        public int ShieldLife;
        protected bool IsControlled;

        public Entity(int id, Complex position, int shieldLife, bool isControlled)
        {
            this.Id = id;
            this.Position = position;
            this.ShieldLife = shieldLife;
            this.IsControlled = isControlled;
        }

        protected int DistanceTo(Entity entity)
        {
            return (int)Complex.Subtract(this.Position, entity.Position).Magnitude;
        }

        protected int DistanceTo(Complex position)
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
        private readonly int ThreatFor;

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
            if ((this.IsThreatForMe() && !((Monster)obj!).IsThreatForMe()) || this.DistanceToMyBase < ((Monster)obj!).DistanceToMyBase)
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
                    if (this.DistanceTo(Party.MyHeroes![heroesIndex[i]]) < this.DistanceTo(Party.MyHeroes[heroesIndex[nearestHeroIndex]]))
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
            foreach (OppHero oppHero in Party.OppHeroes!)
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
        protected Complex AlterVector { get; set; }

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
            for (int i = 0; i < Party.Monsters!.Count; i++)
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
            return (Party.Monsters![target].ShieldLife == 0 && this.DistanceTo(Party.Monsters[target]) <= 1280 && Party.MyMana >= 10 &&
                    (Party.Monsters[target].DistanceToMyBase < 1100
                    || (Party.Monsters[target].DistanceToMyBase < 2600 && Party.Monsters[target].CanBeWinded())
                    || (Party.Turn > 90 && Party.Monsters[target].DistanceToMyBase <= 5000)));
        }

        protected Complex GetDefenseWindVector(int target)
        {
            Complex windVector = Complex.Subtract(Party.Monsters![target].Position, Party.MyBase);
            windVector = Complex.FromPolarCoordinates(2200, windVector.Phase);
            windVector = Complex.Add(this.Position, windVector);

            return windVector;
        }
    }

    /*____________________________________________________________*/
    /*____________________________MYHERO1_________________________*/
    /*____________________________________________________________*/

    public class MyHero1 : MyHero
    {
        private double PhaseShift;
        private readonly int AttackExplorationMagnitude;
        private readonly double MinPhase;
        private readonly double MaxPhase;
        private bool MustJoinShieldPoint;
        private readonly double ShieldPointPhase;

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
                    Complex windVector = this.GetDefenseWindVector((int)urgentTarget);
                    this.Wind(windVector);
                }
                else
                {
                    this.GoTo(Party.Monsters![(int)urgentTarget]);
                }
            }
            else
            {
                int? targetInMyScope = this.GetTargetInScope();

                if (targetInMyScope != null)
                {
                    this.GoTo(Party.Monsters![(int)targetInMyScope]);
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

        public override void Action()
        {
            int? urgentTarget = Party.GetUrgentTarget(MY_HERO2);

            if (urgentTarget != null)
            {
                if (this.MustDefenseWind((int)urgentTarget))
                {
                    Complex windVector = this.GetDefenseWindVector((int)urgentTarget);
                    this.Wind(windVector);
                }
                else
                {
                    this.GoTo(Party.Monsters![(int)urgentTarget]);
                }
            }
            else
            {
                int? targetInMyScope = this.GetTargetInScope();

                if (targetInMyScope != null)
                {
                    this.GoTo(Party.Monsters![(int)targetInMyScope]);
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
                    Complex windVector = this.GetDefenseWindVector((int)urgentTarget);
                    this.Wind(windVector);
                }
                else
                {
                    this.GoTo(Party.Monsters![(int)urgentTarget]);
                }
            }
            else
            {
                int? targetInMyScope = this.GetTargetInScope();

                if (targetInMyScope != null)
                {
                    this.GoTo(Party.Monsters![(int)targetInMyScope]);
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
        inputs = Console.ReadLine()!.Split(' ');

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

        int heroesPerPlayer = int.Parse(Console.ReadLine()!);

        Party.MyHeroes = new List<MyHero>(heroesPerPlayer);
        Party.Turn = 0;

        // game loop
        while (true)
        {
            Party.AddTurn();

            inputs = Console.ReadLine()!.Split(' ');
            Party.MyHealth = int.Parse(inputs[0]);
            Party.MyMana = int.Parse(inputs[1]);

            inputs = Console.ReadLine()!.Split(' ');
            Party.OppHealth = int.Parse(inputs[0]);
            Party.OppMana = int.Parse(inputs[1]);

            int entityCount = int.Parse(Console.ReadLine()!);

            Party.OppHeroes = new List<OppHero>(entityCount);
            Party.Monsters = new List<Monster>(entityCount);

            int heroIndex = 0;

            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine()!.Split(' ');
                int id = int.Parse(inputs[0]); // Unique identifier
                int type = int.Parse(inputs[1]); // 0=monster, 1=your heroIndex, 2=opponent heroIndex
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
                            Party.MyHeroes[heroIndex].Update(new Complex(x, y), shieldLife, (isControlled == 1));
                        }
                        else
                        {
                            switch (heroIndex)
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

                        heroIndex++;

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
        }
    }
}