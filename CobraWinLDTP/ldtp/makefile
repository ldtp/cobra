.SUFFIXES: .java .class
.java.class:
	javac $<

SOURCES = \
	Ldtp.java \
	LdtpExecutionError.java

default: Ldtp.class LdtpExecutionError.class

doc: $(SOURCES)
	javadoc $(SOURCES)

clean:
	$(RM) *.class *~ *.html *.css package-list
