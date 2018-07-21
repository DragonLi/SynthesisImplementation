MONO = mono
MONOC = mcs
MONOCFLAGS = -optimize -reference:${PARSERREF}
GPLEX = ${MONO} gplex.exe
GPPG = ${MONO} gppg.exe
PARSERREF = ShiftReduceParser.dll
CSFILES = Absyn.cs Parser.cs Printer.cs Scanner.cs Test.cs VisitSkeleton.cs AbstractVisitSkeleton.cs AGTransformer.cs
all: test

clean:
	rm -f Calc.pdf test Calc.pdf Calc.aux Calc.log

distclean: clean
	rm -f ${CSFILES}
	rm -f Calc.l Calc.y Calc.tex
	rm -f Makefile

test: Parser.cs Scanner.cs
	@echo "Compiling test..."
	${MONOC} ${MONOCFLAGS} -out:test.exe ${CSFILES}

Scanner.cs: Calc.l
	${GPLEX} /out:$@ Calc.l

Parser.cs: Calc.y
	${GPPG} /gplex Calc.y > $@

pdf: Calc.tex
	pdflatex Calc.tex
