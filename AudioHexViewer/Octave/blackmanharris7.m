function w = blackmanharris7(N)
  n = (0:N-1)';
  if N == 1
    w = 1;
    return;
  end

  % Coefficients Blackman-Harris 7 termes (classiques)
  a0 = 0.27105140069342;
  a1 = 0.43329793923448;
  a2 = 0.21812299954311;
  a3 = 0.06592544638803;
  a4 = 0.01081174209837;
  a5 = 0.00077658482522;
  a6 = 0.00001388721735;

  x = 2*pi*n/(N-1);

  w = a0 ...
    - a1*cos(1*x) ...
    + a2*cos(2*x) ...
    - a3*cos(3*x) ...
    + a4*cos(4*x) ...
    - a5*cos(5*x) ...
    + a6*cos(6*x);
end

