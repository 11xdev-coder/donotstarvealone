extends Node


export(int) var max_health = 1
onready var health = max_health setget sethealth

signal ded

func sethealth(newhealth):
	health = newhealth
	if health <= 0:
		emit_signal("ded")
