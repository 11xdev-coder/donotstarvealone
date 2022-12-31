extends Area2D

var player = null

func canSeePlayer():
	return player != null

func _on_playerdetection_body_entered(body):
	player = body


func _on_playerdetection_body_exited(body):
	player = null
