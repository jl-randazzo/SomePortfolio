def main():
	inputFile = raw_input("Enter input file name: ")
	print(inputFile)
	outputFile = raw_input("Enter output file name: ")
	print(outputFile)
	inpF = open(inputFile, 'r')
	outF = open(outputFile, 'w')
	nextLine = False
	for line in inpF:
		if nextLine:
			nextLine = False
			outF.write(line)
			continue
		if "csv data:" in line:
			nextLine = True
			continue
	inpF.close()
	outF.close()
			
	

if __name__ == "__main__":
	main()
