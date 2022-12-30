extends KinematicBody2D


onready var stats = $Stats


func _on_hurtbox_area_entered(area):
	# area - is object who entered hurtbox
	stats.health -= area.damage
	
func _on_Stats_ded():
	queue_free()
