extends KinematicBody2D


onready var stats = $Stats
onready var playerdetection = $playerdetection
enum {
	idle,
	wander,
	chase
}

export(int) var speed = 150
var vel = Vector2.ZERO
var state = chase


func _physics_process(delta):
	match state:
		idle:
			vel = vel.move_toward(Vector2.ZERO, speed)
			
		wander:
			pass
		chase:
			var player = playerdetection.player
			if player != null:
				var direction = (player.global_position - global_position).normalized()
				vel = vel.move_toward(direction, speed)
		
	vel = move_and_slide(vel)


func seek_player():
	if playerdetection.canSeePlayer():
		state = chase

func _on_hurtbox_area_entered(area):
	# area - is object who entered hurtbox
	stats.health -= area.damage
	
func _on_Stats_ded():
	queue_free()
