using Godot;
using System;
using Kuros.Core;
using Kuros.Actors.Enemies;
using Kuros.Utils;

namespace Kuros.Actors.Heroes
{
	/// <summary>
	/// 主角色控制器，用于控制带有 SpineSprite 动画的 CharacterBody2D 角色
	/// 简化版本，不依赖复杂的状态机系统
	/// 继承自 SamplePlayer 以确保敌人能够正确检测和攻击
	/// </summary>
	public partial class MainCharacter : SamplePlayer
	{
		[ExportCategory("Animation")]
	[Export] public NodePath SpineSpritePath { get; set; } = new NodePath("SpineSprite");
	[Export] public string IdleAnimationName { get; set; } = "idle";
	[Export] public string WalkAnimationName { get; set; } = "walk";
	[Export] public string RunAnimationName { get; set; } = "run";
	[Export] public string AttackAnimationName { get; set; } = "attack";
	[Export] public float WalkAnimationSpeed { get; set; } = 1.5f;
	[Export] public float RunAnimationSpeed { get; set; } = 2.0f;
	[Export] public float RunSpeedMultiplier { get; set; } = 2.0f; // 跑步速度倍数
	[Export] public float AnimationMixDuration { get; set; } = 0.1f; // 动画混合时长

	[ExportCategory("Combat")]
	[Export] public Area2D AttackArea { get; private set; } = null!;

	[ExportCategory("Input")]
	[Export] public string MoveLeftAction { get; set; } = "move_left";
	[Export] public string MoveRightAction { get; set; } = "move_right";
	[Export] public string MoveForwardAction { get; set; } = "move_forward";
	[Export] public string MoveBackAction { get; set; } = "move_back";
	[Export] public string AttackAction { get; set; } = "attack";
	[Export] public string RunAction { get; set; } = "run";

	// Spine 相关（使用 Node 引用，通过 Call 调用 GDScript 方法）
	private Node? _spineController;
	private string _currentAnimation = string.Empty;
	private bool _isAttacking = false;
	private bool _isRunning = false;

	public override void _Ready()
	{
		base._Ready();
		AddToGroup("player");

		// 尝试查找 AttackArea
		if (AttackArea == null)
		{
			AttackArea = GetNodeOrNull<Area2D>("AttackArea");
		}

		// 初始化 SpineSprite 和 AnimationState
		// 注意：SamplePlayer._Ready() 已经会尝试查找 InventoryComponent 和 WeaponSkillController
		// 如果场景中没有这些组件，会有警告但不会影响基本功能
		InitializeSpine();

		// 播放初始待机动画
		PlayAnimation(IdleAnimationName, true);
	}


	/// <summary>
	/// 初始化 SpineController（查找挂载了 SpineController.gd 的 SpineSprite 节点）
	/// </summary>
	private void InitializeSpine()
	{
		// 尝试通过路径获取 SpineSprite 节点
		Node? spineNode = null;
		if (!SpineSpritePath.IsEmpty)
		{
			spineNode = GetNodeOrNull(SpineSpritePath);
		}

		// 如果路径获取失败，尝试按名称查找
		if (spineNode == null)
		{
			spineNode = GetNodeOrNull("SpineSprite");
		}

		// 如果还是找不到，尝试递归查找
		if (spineNode == null)
		{
			spineNode = FindChild("SpineSprite", recursive: true, owned: false);
		}

		if (spineNode == null)
		{
			GD.PushWarning($"[{Name}] 未找到 SpineSprite 节点！请确保场景中有 SpineSprite 子节点，并且挂载了 SpineController.gd 脚本。");
			return;
		}

		// SpineController.gd 应该直接挂载在 SpineSprite 节点上
		_spineController = spineNode;

		// 验证节点是否有 play 方法（即是否挂载了 SpineController.gd）
		if (_spineController != null && _spineController.HasMethod("play"))
		{
			GD.Print($"[{Name}] SpineController 初始化成功");
		}
		else
		{
			GD.PushWarning($"[{Name}] SpineSprite 节点未挂载 SpineController.gd 脚本！请在 SpineSprite 节点上附加 scripts/controllers/SpineController.gd 脚本。");
			_spineController = null;
		}
	}

	public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);

			// 如果正在攻击，不处理移动输入
			if (_isAttacking)
			{
				// 应用摩擦力，逐渐停止
				Velocity = Velocity.MoveToward(Vector2.Zero, Speed * 2 * (float)delta);
				MoveAndSlide();
				return;
			}

			// 获取移动输入
			Vector2 input = GetMovementInput();

			// 检查是否按住跑步键
			_isRunning = Input.IsActionPressed(RunAction);

			// 处理移动
			if (input != Vector2.Zero)
			{
				// 计算速度
				float currentSpeed = _isRunning ? Speed * RunSpeedMultiplier : Speed;
				Velocity = input * currentSpeed;

				// 更新朝向
				if (input.X != 0)
				{
					FlipFacing(input.X > 0);
				}

				// 播放移动动画
				if (_isRunning)
				{
					PlayAnimation(RunAnimationName, true, RunAnimationSpeed);
				}
				else
				{
					PlayAnimation(WalkAnimationName, true, WalkAnimationSpeed);
				}
			}
			else
			{
				// 没有输入，播放待机动画
				Velocity = Velocity.MoveToward(Vector2.Zero, Speed * 2 * (float)delta);
				PlayAnimation(IdleAnimationName, true);
			}

			// 应用移动
			MoveAndSlide();
		}

	public override void _UnhandledInput(InputEvent @event)
		{
			// 处理攻击输入
			if (@event.IsActionPressed(AttackAction) && !_isAttacking && AttackTimer <= 0)
			{
				StartAttack();
				GetViewport().SetInputAsHandled();
			}

			base._UnhandledInput(@event);
		}

		/// <summary>
		/// 获取移动输入向量
		/// </summary>
		private Vector2 GetMovementInput()
		{
			return Input.GetVector(MoveLeftAction, MoveRightAction, MoveForwardAction, MoveBackAction);
		}

		/// <summary>
		/// 播放 Spine 动画（通过调用 SpineController.gd 的 play 方法）
		/// </summary>
		/// <param name="animName">动画名称</param>
		/// <param name="loop">是否循环</param>
		/// <param name="timeScale">时间缩放（播放速度）</param>
		private void PlayAnimation(string animName, bool loop, float timeScale = 1.0f)
		{
			// 如果 SpineController 未初始化，跳过
			if (_spineController == null)
			{
				return;
			}

			// 如果动画已经在播放，跳过
			if (_currentAnimation == animName)
			{
				return;
			}

			_currentAnimation = animName;

			try
			{
				// 调用 SpineController.gd 的 play 方法
				// play(anim: String, loop := true, mix_duration := 0.1, time_scale := 1.0)
				_spineController.Call("play", animName, loop, AnimationMixDuration, timeScale);

				// 如果是非循环动画（如攻击），使用定时器估算动画时长
				if (!loop)
				{
					// 获取动画时长（通过调用 get_state 获取 AnimationState，然后查询动画时长）
					// 这里使用固定时长作为后备方案
					float estimatedDuration = EstimateAnimationDuration(animName);
					GetTree().CreateTimer(estimatedDuration).Timeout += OnAnimationComplete;
				}
			}
			catch (Exception ex)
			{
				GD.PushWarning($"[{Name}] 播放动画失败: {animName}, 错误: {ex.Message}");
			}
		}

		/// <summary>
		/// 估算动画时长（用于非循环动画的完成检测）
		/// </summary>
		private float EstimateAnimationDuration(string animName)
		{
			// 可以根据动画名称返回不同的估算时长
			// 这里使用默认值，实际项目中可以通过配置文件或动画数据获取真实时长
			if (animName == AttackAnimationName)
			{
				return 0.5f; // 攻击动画通常较短
			}
			return 1.0f; // 默认 1 秒
		}

		/// <summary>
		/// 动画播放完成回调（用于非循环动画，如攻击）
		/// </summary>
		private void OnAnimationComplete()
		{
			// 只处理攻击动画的完成事件
			if (_isAttacking && _currentAnimation == AttackAnimationName)
			{
				OnAttackAnimationFinished();
			}
		}

		/// <summary>
		/// 开始攻击
		/// </summary>
		private void StartAttack()
	{
		_isAttacking = true;
		AttackTimer = AttackCooldown;

		// 播放攻击动画（非循环）
		PlayAnimation(AttackAnimationName, false);

		// 执行攻击检测
		PerformAttackCheck();
	}

		/// <summary>
		/// 攻击动画完成回调
		/// </summary>
		private void OnAttackAnimationFinished()
	{
		_isAttacking = false;

		// 恢复待机动画
		PlayAnimation(IdleAnimationName, true);
	}

		/// <summary>
		/// 执行攻击检测
		/// </summary>
		private void PerformAttackCheck()
		{
			if (AttackArea == null)
			{
				GD.PushWarning($"[{Name}] AttackArea 未设置，无法执行攻击检测");
				return;
			}

			var bodies = AttackArea.GetOverlappingBodies();
			foreach (var body in bodies)
			{
				if (body is SampleEnemy enemy)
				{
					enemy.TakeDamage((int)AttackDamage, GlobalPosition, this);
					GameLogger.Info(nameof(MainCharacter), $"击中敌人: {enemy.Name}");
				}
			}
		}

	public override void TakeDamage(int damage, Vector2? attackOrigin = null, GameActor? attacker = null)
	{
		base.TakeDamage(damage, attackOrigin, attacker);
		
		// 受伤时停止攻击
		if (_isAttacking)
		{
			_isAttacking = false;
			// 注意：定时器回调无法直接取消，但会在回调中检查 _isAttacking 状态
		}
	}

	protected override void OnDeathFinalized()
		{
			EffectController?.ClearAll();
			GameLogger.Warn(nameof(MainCharacter), "角色死亡！");
			// 可以在这里添加游戏结束逻辑
			// GetTree().ReloadCurrentScene();
		}
	}
}
