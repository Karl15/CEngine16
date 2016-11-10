CEngine16

FPGAs are popular as accelerators because of computation speed, but require FPGA reconfiguration(painful and time consuming) to change the algorithm.

Embedded soft CPUs on FPGAs are slow and take a lot of resources so are not suited for accelerators.

CEngine is programmable by loading memory.  Unlike an accelerator, it is general purpose C rather than just a computational algorithm accelerator.  Also fast and takes less cycles than a CPU.

The general cycle sequence: 
2 cycles to start plus 1 cycle per operator.

Assignment:
1) Read addresses of first 2 operands.
2) Read first 2 operands and operator control word: Perform the operation.  End and write result or do next operator.
Repeat general sequence.

Condition evaluation:
1) Read addresses of 2 operands.
2) Read the 2 operands and either continue or read from target address.
Repeat general sequence.

 Operands default to unsigned integers that are kept in a true dual port local memory and operators are in control words.
 Other data types require design extensions as do external memory(if required).
 
 Peripheral interfaces are TBD, probably MMIO like using operand addresses.
