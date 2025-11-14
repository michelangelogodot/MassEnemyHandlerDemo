using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class MoneyShower : VBoxContainer


{
	[Export]
	private MassEnemySystem EnemySystem;
	private int amount_of_currency_types = Enum.GetValues(typeof(MassEnemySystem.CurrencyTypes)).Length;
	Label[] cur_labels;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {	
		EnemySystem.CurenciesChanged += OnCurrencyChanged;

		cur_labels = new Label[amount_of_currency_types];
		for (int j = 0; j < GetChildCount(); j++)
        {
            GetChild(j).QueueFree();
        }
		for (int i = 0; i < amount_of_currency_types; i++)
        {

			cur_labels[i] = new Label();
            cur_labels[i].Text = "0";
			AddChild(cur_labels[i]);
        }

    }

	public void OnCurrencyChanged()
    {
        for (int i = 0; i < amount_of_currency_types; i++)
        {
            int this_cur_count = MassEnemySystem.CurrencyBuffer[i];
			
			cur_labels[i].Text = $"{this_cur_count} {Enum.GetNames(typeof(MassEnemySystem.CurrencyTypes))[i]}" + " CURRENCY";

        }
    }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        
    }
}
