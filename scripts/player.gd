extends KinematicBody2D

var p_vel = Vector2.ZERO
const p_MAXSPEED = 25
const v2zero = Vector2.ZERO

onready var animationPlayer = $animation
onready var treeanim = $treeanim
onready var animState = treeanim.get("parameters/playback")

enum {
	move,
	attack
}

var state = move

func _ready():
	treeanim.active = true
	get_node("attackboxpivot/attackbox/CollisionShape2D").disabled = true  

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta):
	match state:
		move:
			state_move(delta)
		attack:
			state_attack(delta)
	
	
func state_move(delta):
	var input_vector = v2zero
	input_vector.x = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
	input_vector.y = Input.get_action_strength("ui_down") - Input.get_action_strength("ui_up")
	input_vector = input_vector.normalized()
	if input_vector != v2zero:
		treeanim.set("parameters/walk/blend_position", input_vector)
		treeanim.set("parameters/idle/blend_position", input_vector)
		treeanim.set("parameters/attack/blend_position", input_vector)
		animState.travel("walk")
		p_vel = input_vector
	else:
		animState.travel("idle")
		p_vel = v2zero
		
	p_vel = move_and_slide(p_vel * p_MAXSPEED)
	
	if Input.is_action_just_pressed("ui_attack"):
		state = attack
	
func state_attack(delta):
	p_vel = v2zero
	animState.travel("attack")
	
func state_attack_finished():
	state = move









