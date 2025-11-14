using Godot;
using System;

public partial class Cursor : MeshInstance2D
{
    [Export]
    private MassEnemySystem EnemySystem;

    [Export]
    private Camera2D Camera;

    private enum AttackType { Radius, AABB, OBB }
    private AttackType currentAttack = AttackType.Radius;

    private float attackRadius = 100f;
    private Vector2 aabbSize = new Vector2(200, 200);
    private Vector2 obbSize = new Vector2(200, 200);

    // Rotation for OBB in radians
    private float obbRotation = 0.0f;

    // Rotation speed in radians per second
    private float obbRotationSpeed = Mathf.Pi / 4f; // 22.5 degrees per second

    private SphereMesh radiusMesh;
    private QuadMesh boxMesh;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        // Prepare meshes
        radiusMesh = new SphereMesh
        {
            Radius = attackRadius,
			Height = attackRadius * 2
        };

        boxMesh = new QuadMesh
        {
            Size = aabbSize // size will be scaled via Transform
        };

        UpdateMesh();
    }

    private void UpdateMesh()
    {
        switch (currentAttack)
        {
            case AttackType.Radius:
                Mesh = radiusMesh;
                Scale = Vector2.One;
                Rotation = 0f;
                break;
            case AttackType.AABB:
				boxMesh.Size = aabbSize;
                Mesh = boxMesh;
                Rotation = 0f;
                break;
            case AttackType.OBB:
                Mesh = boxMesh;
				boxMesh.Size = obbSize;
                Mesh = boxMesh;
                Rotation = obbRotation;
                break;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("zoom_in"))
        {
            Camera.Zoom += Vector2.One * 0.1f;
        }
        else if (@event.IsActionPressed("zoom_out"))
        {
            Camera.Zoom -= Vector2.One * 0.1f;
        }
        else if (@event.IsActionPressed("next"))
        {
            // Cycle through attack types
            currentAttack = (AttackType)(((int)currentAttack + 1) % Enum.GetNames(typeof(AttackType)).Length);
            UpdateMesh();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Position = GetGlobalMousePosition();

        // Rotate OBB if currently selected
        if (currentAttack == AttackType.OBB)
        {
            obbRotation += obbRotationSpeed * dt;
            Rotation = obbRotation; // visually rotate mesh
        }

        switch (currentAttack)
        {
            case AttackType.Radius:
                EnemySystem.GetEnemiesInRadius(Position, attackRadius);
                break;
            case AttackType.AABB:
                EnemySystem.GetEnemiesInAABB(Position - aabbSize / 2, Position + aabbSize / 2);
                break;
            case AttackType.OBB:
                EnemySystem.GetEnemiesInOBB(Position, obbSize, obbRotation);
                break;
        }

        EnemySystem.DamageEnemiesInBuffer(5);
    }
}
