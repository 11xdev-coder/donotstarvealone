extends KinematicBody2D

var p_vel = Vector2.ZERO
const p_MAXSPEED = 25
const v2zero = Vector2.ZERO

onready var animationPlayer = $animation



# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta):
	var input_vector = v2zero
	input_vector.x = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
	input_vector.y = Input.get_action_strength("ui_down") - Input.get_action_strength("ui_up")
	input_vector = input_vector.normalized()
	if input_vector != v2zero:
		if input_vector.x > 0:
			animationPlayer.play("walkright")
		else:
			animationPlayer.play("walkleft")
		p_vel = input_vector
	else:
		animationPlayer.stop()
		p_vel = v2zero
		
	p_vel = move_and_slide(p_vel * p_MAXSPEED)
