extends KinematicBody2D

var p_MAXSPEED = 50
var p_vel = Vector2.ZERO
var v2zero = Vector2.ZERO


# Declare member variables here. Examples:
# var a = 2
# var b = "text"


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
		
	move_and_collide(p_vel * delta * p_MAXSPEED)
