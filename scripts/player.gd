extends KinematicBody2D

var p_vel = Vector2.ZERO
const p_MAXSPEED = 25
const v2zero = Vector2.ZERO


# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta):
	var input_vector = v2zero
	input_vector.x = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
	input_vector.y = Input.get_action_strength("ui_down") - Input.get_action_strength("ui_up")
	input_vector = input_vector.normalized()
	if input_vector != v2zero:
		p_vel = input_vector
	else:
		p_vel = v2zero
		
	p_vel = move_and_slide(p_vel * p_MAXSPEED)
