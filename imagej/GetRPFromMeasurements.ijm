macro "GetRP [r]" {
	area = getResult("Area", nResults-5);
	diameter = sqrt(area / 3.14) * 2;
	disp = getResult("X", nResults-4) - getResult("X", nResults-3) + getResult("X", nResults-1) - getResult("X", nResults-2);
	rp = disp / 0.349;
	print(d2s(diameter,2), "\t", d2s(rp,2));
}
