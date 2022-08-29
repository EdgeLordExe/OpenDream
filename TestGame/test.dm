namespace test{
/datum/test
	var/name = "k"

/datum/test/proc/foo()
	return "bar"

/datum/test/proc/foobar2()
	return foo() + bar()
}

test::/datum/test/proc/bar()
	return "foo"

test::datum/test/proc/foobar()
	return bar() + foo()

datum/test/proc/foo()
	return 1

datum/test/proc/foobar()
	return foo()
