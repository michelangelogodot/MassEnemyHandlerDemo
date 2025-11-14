using Godot;
using System;

public partial class MassEnemySystem : MultiMeshInstance2D
{

	const float DISTANCE_RIGHT = 1152.0f / 2;
	const float DISTANCE_LEFT = -1152.0f / 2;
	const float DISTANCE_TOP = -648.0f / 2;
	const float DISTANCE_BOTTOM = 648.0f / 2;
    public const int ENEMY_COUNT = 50000;
	public const float ENEMY_RADIUS = 2f;
	public const float HIT_DECAY_RATE = 3.0f;
    [Signal]
    public delegate void CurenciesChangedEventHandler();

    private static readonly int INSTANCES_BUFFER_STRIDE = Enum.GetNames(typeof(InstanceBufferIndexes)).Length;
    private static readonly int ENEMY_BUFFER_STRIDE = Enum.GetNames(typeof(EnemyBufferIndexes)).Length;
    private static readonly int AMOUNT_OF_ENEMY_TYPES = Enum.GetValues(typeof(EnemyType)).Length;

    private readonly float[] PrevInstancesBuffer = new float[ENEMY_COUNT * INSTANCES_BUFFER_STRIDE];
    private readonly float[] CurInstancesBuffer = new float[ENEMY_COUNT * INSTANCES_BUFFER_STRIDE];
	private readonly float[] EnemyBuffer = new float[ENEMY_COUNT * ENEMY_BUFFER_STRIDE];
	public static int AmountOfAngles = Engine.PhysicsTicksPerSecond * 3;
	private readonly float[] EnemyAngles = new float[AmountOfAngles];
	private readonly float[] EnemyCos = new float[AmountOfAngles];
	private readonly float[] EnemySin = new float[AmountOfAngles];
	private readonly int[] _hitBuffer = new int[ENEMY_COUNT];
 	public static readonly int[] CurrencyBuffer = new int[Enum.GetNames(typeof(CurrencyTypes)).Length];

	private int _hitCount = 0;
	private RandomNumberGenerator rng = new RandomNumberGenerator();
    public enum InstanceBufferIndexes
    {
        M11, M12, PAD1, TX,
        M21, M22, PAD2, TY,
        C_R, C_G, C_B, C_A
    }
	public enum EnemyBufferIndexes
	{
		POS_X, POS_Y, VEL_X, VEL_Y,
		DEFAULT_COLOR_R, DEFAULT_COLOR_G, DEFAULT_COLOR_B,
		HIT_COLOR_R, HIT_COLOR_G, HIT_COLOR_B,
		CRIT_HIT_COLOR_R, CRIT_HIT_COLOR_G, CRIT_HIT_COLOR_B,
		DEFAULT_SCALE,
		HIT_SCALE,
		START_ANGLE,
		IS_CRIT,
		HIT_STATE_FACTOR,
		DROPPED_CURRENCY,
	};

	public enum CurrencyTypes { BLUE, GREEN, RED };

	public enum EnemyType { BLUE, GREEN, RED };

	public struct EnemyData {
		public Color DefaultColor;
		public Color HitColor;
		public Color CritHitColor;
		public float DefaultScale;
		public float HitScale;
		public float Speed;
		public CurrencyTypes DroppedCurrency;

    }

	EnemyData[] EnemyDataContainer = new EnemyData[AMOUNT_OF_ENEMY_TYPES];

	public override void _Ready()
	{
		_CalculateAllEnemyAngles();
		_SetupMultiMesh();
		_SetupEnemyData();
		_SpawnEnemies();
	}

	private void _CalculateAllEnemyAngles()
	{
		float angleDelta = Mathf.Tau / AmountOfAngles;

		for (int i = 0; i < AmountOfAngles; i++)
		{
			float angle = angleDelta * i;
			EnemyAngles[i] = angle;
			EnemyCos[i] = Mathf.Cos(angle);
			EnemySin[i] = Mathf.Sin(angle);
		}
	}



	private void _SpawnEnemies()
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		for (int i = 0; i < ENEMY_COUNT; i++)
		{
			int Id = i % AMOUNT_OF_ENEMY_TYPES;
			EnemyData ThisEnemyData = EnemyDataContainer[Id];

			float XPos = rng.RandfRange(DISTANCE_LEFT, DISTANCE_RIGHT);
			float YPos = rng.RandfRange(DISTANCE_BOTTOM, DISTANCE_TOP);
			Vector2 Vel = new Vector2(rng.Randf() - 0.5f, rng.Randf() - 0.5f);
			if (Vel.LengthSquared() == 0)
			{
				Vel = new Vector2(1.0f, 0.0f);
			}
			Vel = Vel.Normalized() * ThisEnemyData.Speed;

			int EnemyBase = i * ENEMY_BUFFER_STRIDE;
			EnemyBuffer[(int)EnemyBufferIndexes.POS_X + EnemyBase] = XPos;
			EnemyBuffer[(int)EnemyBufferIndexes.POS_Y + EnemyBase] = YPos;
			EnemyBuffer[(int)EnemyBufferIndexes.VEL_X + EnemyBase] = Vel.X;
			EnemyBuffer[(int)EnemyBufferIndexes.VEL_Y + EnemyBase] = Vel.Y;
			EnemyBuffer[(int)EnemyBufferIndexes.DEFAULT_COLOR_R + EnemyBase] = ThisEnemyData.DefaultColor.R;
			EnemyBuffer[(int)EnemyBufferIndexes.DEFAULT_COLOR_G + EnemyBase] = ThisEnemyData.DefaultColor.G;
			EnemyBuffer[(int)EnemyBufferIndexes.DEFAULT_COLOR_B + EnemyBase] = ThisEnemyData.DefaultColor.B;
			EnemyBuffer[(int)EnemyBufferIndexes.HIT_COLOR_R + EnemyBase] = ThisEnemyData.HitColor.R;
			EnemyBuffer[(int)EnemyBufferIndexes.HIT_COLOR_G + EnemyBase] = ThisEnemyData.HitColor.G;
			EnemyBuffer[(int)EnemyBufferIndexes.HIT_COLOR_B + EnemyBase] = ThisEnemyData.HitColor.B;
			EnemyBuffer[(int)EnemyBufferIndexes.CRIT_HIT_COLOR_R + EnemyBase] = ThisEnemyData.CritHitColor.R;
			EnemyBuffer[(int)EnemyBufferIndexes.CRIT_HIT_COLOR_G + EnemyBase] = ThisEnemyData.CritHitColor.G;
			EnemyBuffer[(int)EnemyBufferIndexes.CRIT_HIT_COLOR_B + EnemyBase] = ThisEnemyData.CritHitColor.B;
			EnemyBuffer[(int)EnemyBufferIndexes.DEFAULT_SCALE + EnemyBase] = ThisEnemyData.DefaultScale;
			EnemyBuffer[(int)EnemyBufferIndexes.HIT_SCALE + EnemyBase] = ThisEnemyData.HitScale;
			EnemyBuffer[(int)EnemyBufferIndexes.IS_CRIT + EnemyBase] = 0;
			EnemyBuffer[(int)EnemyBufferIndexes.HIT_STATE_FACTOR + EnemyBase] = 0;
			EnemyBuffer[(int)EnemyBufferIndexes.DROPPED_CURRENCY + EnemyBase] = (float)ThisEnemyData.DroppedCurrency;
			EnemyBuffer[(int)EnemyBufferIndexes.START_ANGLE + EnemyBase] = rng.RandiRange(0, AmountOfAngles - 1);

			int InstancesBase = i * INSTANCES_BUFFER_STRIDE;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.C_A] = 1;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.C_R] = ThisEnemyData.DefaultColor.R;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.C_G] = ThisEnemyData.DefaultColor.G;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.C_B] = ThisEnemyData.DefaultColor.B;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M11] = EnemyCos[Mathf.RoundToInt(EnemyBuffer[(int)EnemyBufferIndexes.START_ANGLE + EnemyBase])] * ThisEnemyData.DefaultScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M12] = -EnemySin[Mathf.RoundToInt(EnemyBuffer[(int)EnemyBufferIndexes.START_ANGLE + EnemyBase])] * ThisEnemyData.DefaultScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M21] = EnemySin[Mathf.RoundToInt(EnemyBuffer[(int)EnemyBufferIndexes.START_ANGLE + EnemyBase])] * ThisEnemyData.DefaultScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M22] = EnemyCos[Mathf.RoundToInt(EnemyBuffer[(int)EnemyBufferIndexes.START_ANGLE + EnemyBase])] * ThisEnemyData.DefaultScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.PAD2] = 1;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.PAD1] = 1;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TY] = YPos;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TX] = XPos;

		}
		Multimesh.Buffer = CurInstancesBuffer;
	}
	


	private void _SetupMultiMesh()
	{
		MultiMesh mm = new MultiMesh();
		mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
		mm.UseColors = true;
		mm.InstanceCount = ENEMY_COUNT;
		mm.Mesh = _CreateFlatPolygon(3);
		mm.PhysicsInterpolationQuality = MultiMesh.PhysicsInterpolationQualityEnum.Fast;
		Multimesh = mm;

	}
	private void _SetupEnemyData()
	{
		EnemyDataContainer[(int)EnemyType.BLUE] = new EnemyData
		{
			DefaultColor = new Color(0.2f, 0.4f, 1.0f),
			HitColor = new Color(1f, 1f, 1.0f),
			CritHitColor = new Color(1.0f, 0.6f, 0f),
			DefaultScale = 1.0f,
			HitScale = 0.7f,
			Speed = 10.0f,
			DroppedCurrency = CurrencyTypes.BLUE
		};

		EnemyDataContainer[(int)EnemyType.GREEN] = new EnemyData
		{
			DefaultColor = new Color(0.2f, 1.0f, 0.4f),
			HitColor = new Color(1f, 1.0f, 1f),
			CritHitColor = new Color(1.0f, 0.6f, 0f),
			DefaultScale = 1.0f,
			HitScale = 0.7f,
			Speed = 10.0f,
			DroppedCurrency = CurrencyTypes.GREEN
		};

		EnemyDataContainer[(int)EnemyType.RED] = new EnemyData
		{
			DefaultColor = new Color(1.0f, 0.3f, 0.3f),
			HitColor = new Color(1.0f, 1f, 1f),
			CritHitColor = new Color(1.0f, 0.6f, 0f),
			DefaultScale = 1.0f,
			HitScale = 0.7f,
			Speed = 10.0f,
			DroppedCurrency = CurrencyTypes.RED
		};
	}

	private ArrayMesh _CreateFlatPolygon(int EdgeCount)
	{
		EdgeCount = Math.Max(3, EdgeCount);
		Vector3[] vertices = new Vector3[EdgeCount];
		int[] indices = new int[EdgeCount];
		if (EdgeCount == 3)
		{
			for (int i = 0; i < EdgeCount; i++)
			{
				float angle = float.Tau * i / EdgeCount - float.Tau / 4;
				vertices[i] = new Vector3(Mathf.Cos(angle) * ENEMY_RADIUS, Mathf.Sin(angle) * ENEMY_RADIUS, 0);
				indices[i] = i;
			}
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);

		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;

	}



	public override void _PhysicsProcess(double delta)
	{

		float dt = (float)delta;
		EmitSignal(SignalName.CurenciesChanged);
		Buffer.BlockCopy(CurInstancesBuffer, 0, PrevInstancesBuffer, 0, CurInstancesBuffer.Length * sizeof(float));
		for (int i = 0; i < ENEMY_COUNT; i++)
		{
			int EnemyBase = i * ENEMY_BUFFER_STRIDE;
			int InstancesBase = i * INSTANCES_BUFFER_STRIDE;

			// Update positions
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TX] +=
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.VEL_X] * dt;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TY] +=
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.VEL_Y] * dt;

			// Bounce on X boundaries
			if (CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TX] > DISTANCE_RIGHT)
			{
				CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TX] = DISTANCE_RIGHT;
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.VEL_X] *= -1f;
			}
			else if (CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TX] < DISTANCE_LEFT)
			{
				CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TX] = DISTANCE_LEFT;
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.VEL_X] *= -1f;
			}

			// Bounce on Y boundaries
			if (CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TY] > DISTANCE_BOTTOM)
			{
				CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TY] = DISTANCE_BOTTOM;
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.VEL_Y] *= -1f;
			}
			else if (CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TY] < DISTANCE_TOP)
			{
				CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TY] = DISTANCE_TOP;
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.VEL_Y] *= -1f;
			}

			float DefaultScale = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.DEFAULT_SCALE];
			int StartAngle = (int)EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.START_ANGLE];

			float HitScale = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.HIT_SCALE];
			float HitStateFactor = Mathf.Clamp(EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.HIT_STATE_FACTOR], 0, 1);
			float CurHitScale = HitScale * HitStateFactor;
			float AppliedScale = DefaultScale + CurHitScale;

			float defaultR = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.DEFAULT_COLOR_R];
			float defaultG = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.DEFAULT_COLOR_G];
			float defaultB = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.DEFAULT_COLOR_B];

			float hitR = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.HIT_COLOR_R];
			float hitG = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.HIT_COLOR_G];
			float hitB = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.HIT_COLOR_B];

			float critHitR = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.CRIT_HIT_COLOR_R];
			float critHitG = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.CRIT_HIT_COLOR_G];
			float critHitB = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.CRIT_HIT_COLOR_B];

			float isCrit = EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.IS_CRIT]; // 1 = crit, 0 = normal

			float r, g, b;

			if (isCrit > 0.5f) // treat >0.5 as true
			{
				// Interpolate between default color and crit hit color
				r = defaultR + (critHitR - defaultR) * HitStateFactor;
				g = defaultG + (critHitG - defaultG) * HitStateFactor;
				b = defaultB + (critHitB - defaultB) * HitStateFactor;
			}
			else
			{
				// Interpolate between default color and normal hit color
				r = defaultR + (hitR - defaultR) * HitStateFactor;
				g = defaultG + (hitG - defaultG) * HitStateFactor;
				b = defaultB + (hitB - defaultB) * HitStateFactor;
			}

			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M11] = EnemyCos[StartAngle] * AppliedScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M12] = -EnemySin[StartAngle] * AppliedScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M21] = EnemySin[StartAngle] * AppliedScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.M22] = EnemyCos[StartAngle] * AppliedScale;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.C_R] = r;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.C_G] = g;
			CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.C_B] = b;
			EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.POS_X] = CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TX];
			EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.POS_Y] = CurInstancesBuffer[InstancesBase + (int)InstanceBufferIndexes.TY];


			if (StartAngle + 1 == AmountOfAngles)
			{
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.START_ANGLE] = 0;
			}
			else
			{
				EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.START_ANGLE] = StartAngle + 1;
			}

			EnemyBuffer[EnemyBase + (int)EnemyBufferIndexes.HIT_STATE_FACTOR] = HitStateFactor - HIT_DECAY_RATE * dt;

		}
		Multimesh.SetBufferInterpolated(CurInstancesBuffer, PrevInstancesBuffer);
	}




public void GetEnemiesInRadius(Vector2 center, float radius)
{
    float radiusSq = radius * radius;
    _hitCount = 0;

    for (int i = 0; i < ENEMY_COUNT; i++)
    {
        int baseIdx = i * ENEMY_BUFFER_STRIDE;
        float dx = EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.POS_X] - center.X;
        float dy = EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.POS_Y] - center.Y;

        if (dx * dx + dy * dy <= radiusSq)
            _hitBuffer[_hitCount++] = i;
    }
}

public void GetEnemiesInAABB(Vector2 topLeft, Vector2 bottomRight)
{
    _hitCount = 0;

    for (int i = 0; i < ENEMY_COUNT; i++)
    {
        int baseIdx = i * ENEMY_BUFFER_STRIDE;
        float x = EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.POS_X];
        float y = EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.POS_Y];

        if (x >= topLeft.X && x <= bottomRight.X &&
            y >= topLeft.Y && y <= bottomRight.Y)
        {
            _hitBuffer[_hitCount++] = i;
        }
    }
}

public void GetEnemiesInOBB(Vector2 center, Vector2 size, float rotation)
{
    _hitCount = 0;
    float cos = Mathf.Cos(-rotation);
    float sin = Mathf.Sin(-rotation);
    float halfWidth = size.X / 2f;
    float halfHeight = size.Y / 2f;

    for (int i = 0; i < ENEMY_COUNT; i++)
    {
        int baseIdx = i * ENEMY_BUFFER_STRIDE;
        float dx = EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.POS_X] - center.X;
        float dy = EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.POS_Y] - center.Y;

        // Transform enemy position into box-local coordinates
        float localX = dx * cos - dy * sin;
        float localY = dx * sin + dy * cos;

        if (localX >= -halfWidth && localX <= halfWidth &&
            localY >= -halfHeight && localY <= halfHeight)
        {
            _hitBuffer[_hitCount++] = i;
        }
    }

}

public void DamageEnemiesInBuffer(int damage)
{
    

    for (int i = 0; i < _hitCount; i++)
    {
        int baseIdx = _hitBuffer[i] * ENEMY_BUFFER_STRIDE;
		bool isCrit = (rng.Randi() % 2 == 0);

        EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.HIT_STATE_FACTOR] = 1f;
        EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.IS_CRIT] = isCrit ? 1f : 0f;
		CurrencyBuffer[(int)EnemyBuffer[baseIdx + (int)EnemyBufferIndexes.DROPPED_CURRENCY]] += isCrit ? damage * 2 : damage;

    }

    // Reset the hit buffer and count
    _hitCount = 0;
    Array.Clear(_hitBuffer, 0, _hitBuffer.Length);
}

}
